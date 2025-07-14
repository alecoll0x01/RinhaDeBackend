using RinhaDeBackend.Models;
using System.Text;
using System.Text.Json;
using StackExchange.Redis;

namespace RinhaDeBackend.Service
{
    public class PaymentProcessorService : IPaymentProcessorService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<PaymentProcessorService> _logger;

        public PaymentProcessorService(
            IHttpClientFactory httpClientFactory,
            IConnectionMultiplexer redis,
            ILogger<PaymentProcessorService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _redis = redis;
            _logger = logger;
        }

        public async Task<(bool success, string? errorMessage)> ProcessPaymentAsync(Guid correlationId, decimal amount, string processorType)
        {
            try
            {
                var client = _httpClientFactory.CreateClient(processorType);

                var request = new ProcessorPaymentRequest
                {
                    CorrelationId = correlationId,
                    Amount = amount,
                    RequestedAt = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("/payments", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Payment {CorrelationId} processed successfully with {ProcessorType}",
                        correlationId, processorType);
                    return (true, null);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Payment {CorrelationId} failed with {ProcessorType}: {StatusCode} - {Error}",
                        correlationId, processorType, response.StatusCode, errorContent);
                    return (false, $"HTTP {response.StatusCode}: {errorContent}");
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogWarning("Payment {CorrelationId} timed out with {ProcessorType}",
                    correlationId, processorType);
                return (false, "Request timeout");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment {CorrelationId} with {ProcessorType}",
                    correlationId, processorType);
                return (false, ex.Message);
            }
        }

        public async Task<ProcessorHealthResponse?> GetHealthAsync(string processorType)
        {
            try
            {
                var db = _redis.GetDatabase();
                var cacheKey = $"health:{processorType}";

                var cachedHealth = await db.StringGetAsync(cacheKey);
                if (cachedHealth.HasValue)
                {
                    return JsonSerializer.Deserialize<ProcessorHealthResponse>(cachedHealth!, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                }

                var client = _httpClientFactory.CreateClient(processorType);
                var response = await client.GetAsync("/payments/service-health");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var health = JsonSerializer.Deserialize<ProcessorHealthResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    await db.StringSetAsync(cacheKey, JsonSerializer.Serialize(health), TimeSpan.FromSeconds(4));

                    return health;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("Health check rate limit exceeded for {ProcessorType}", processorType);
                    return null;
                }
                else
                {
                    _logger.LogWarning("Health check failed for {ProcessorType}: {StatusCode}",
                        processorType, response.StatusCode);
                    return new ProcessorHealthResponse { Failing = true, MinResponseTime = 1000 };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting health for {ProcessorType}", processorType);
                return new ProcessorHealthResponse { Failing = true, MinResponseTime = 1000 };
            }
        }
    }
}
