using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace RinhaDeBackend.Service
{
    public class HealthCheckService : IHealthCheckService
    {
        private readonly IPaymentProcessorService _processorService;
        private readonly ILogger<HealthCheckService> _logger;

        public HealthCheckService(
            IPaymentProcessorService processorService,
            ILogger<HealthCheckService> logger)
        {
            _processorService = processorService;
            _logger = logger;
        }

        public async Task<string> GetBestProcessorAsync()
        {
            try
            {
                var defaultHealthTask = _processorService.GetHealthAsync("default");
                var fallbackHealthTask = _processorService.GetHealthAsync("fallback");

                await Task.WhenAll(defaultHealthTask, fallbackHealthTask);

                var defaultHealth = defaultHealthTask.Result;
                var fallbackHealth = fallbackHealthTask.Result;

                if (defaultHealth != null && !defaultHealth.Failing)
                {
                    return "default";
                }
                if (fallbackHealth != null && !fallbackHealth.Failing)
                {
                    _logger.LogInformation("Using fallback processor - default is failing");
                    return "fallback";
                }
                _logger.LogWarning("Both processors may be failing, using default");
                return "default";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining best processor, defaulting to default");
                return "default";
            }
        }
    }
}
