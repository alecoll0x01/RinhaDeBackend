using RinhaDeBackend.Models;
using System.Text.Json;
using System.Text;

namespace RinhaDeBackend.Services
{
    public class PaymentProcessorService : IPaymentProcessorService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentProcessorService> _logger;
        private readonly Dictionary<string, DateTime> _lastHealthCheck = new();
        private readonly Dictionary<string, ServiceHealthResponse> _healthCache = new();


        public PaymentProcessorService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<PaymentProcessorService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }
        public async Task<ServiceHealthResponse?> GetServiceHealthAsync(string processorType)
        {
            var now = DateTime.UtcNow;
            var key = $"health_{processorType}";

            if (_lastHealthCheck.ContainsKey(key))
            {
                var timeSinceLastCheck = now - _lastHealthCheck[key];
                if (timeSinceLastCheck.TotalSeconds < 5)
                {
                    return _healthCache.GetValueOrDefault(key);
                }
            }

            var baseUrl = GetProcessorUrl(processorType);
            var url = $"{baseUrl}/payments/service-health";

            try
            {
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var healthResponse = JsonSerializer.Deserialize<ServiceHealthResponse>(jsonResponse,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                    _lastHealthCheck[key] = now;
                    _healthCache[key] = healthResponse!;

                    return healthResponse;
                }

                _logger.LogWarning($"Failed to get health status for {processorType} processor");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting health status for {processorType} processor");
                return null;
            }
        }


        public async Task<bool> ProcessPaymentAsync(PaymentProcessorRequest request, string processorType)
        {
            var baseUrl = GetProcessorUrl(processorType);
            var url = $"{baseUrl}/payments";

            try
            {
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Payment processed successfully via {processorType} processor");
                    return true;
                }

                _logger.LogWarning($"Payment failed via {processorType} processor. Status: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing payment via {processorType} processor");
                return false;
            }
        }

        private string GetProcessorUrl(string processorType)
        {
            return processorType switch
            {
                "default" => _configuration["PaymentProcessors:Default"] ?? "http://default-processor:8080",
                "fallback" => _configuration["PaymentProcessors:Fallback"] ?? "http://fallback-processor:8080",
                _ => throw new ArgumentException($"Unknown processor type: {processorType}")
            };
        }

    }
}
