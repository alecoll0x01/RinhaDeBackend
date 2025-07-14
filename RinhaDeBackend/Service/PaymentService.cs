using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RinhaDeBackend.Data;
using RinhaDeBackend.Models;

namespace RinhaDeBackend.Service
{
    public class PaymentService : IPaymentService
    {
        private readonly PaymentContext _context;
        private readonly IPaymentProcessorService _processorService;
        private readonly IHealthCheckService _healthCheckService;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            PaymentContext context,
            IPaymentProcessorService processorService,
            IHealthCheckService healthCheckService,
            ILogger<PaymentService> logger)
        {
            _context = context;
            _processorService = processorService;
            _healthCheckService = healthCheckService;
            _logger = logger;
        }

        public async Task<bool> ProcessPaymentAsync(PaymentRequest request)
        {
            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.CorrelationId == request.CorrelationId);

            if (existingPayment != null)
            {
                _logger.LogWarning("Payment {CorrelationId} already exists", request.CorrelationId);
                return existingPayment.Success;
            }
            var processorType = await _healthCheckService.GetBestProcessorAsync();

            var payment = new Payment
            {
                CorrelationId = request.CorrelationId,
                Amount = request.Amount,
                RequestedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow,
                ProcessorType = processorType,
                Success = false
            };

            try
            {
                var (success, errorMessage) = await _processorService.ProcessPaymentAsync(
                    request.CorrelationId, request.Amount, processorType);

                if (success)
                {
                    payment.Success = true;
                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();
                    return true;
                }
                else
                {
                    payment.ErrorMessage = errorMessage;
                    if (processorType == "default")
                    {
                        _logger.LogInformation("Trying fallback processor for payment {CorrelationId}", request.CorrelationId);

                        var (fallbackSuccess, fallbackError) = await _processorService.ProcessPaymentAsync(
                            request.CorrelationId, request.Amount, "fallback");

                        if (fallbackSuccess)
                        {
                            payment.ProcessorType = "fallback";
                            payment.Success = true;
                            payment.ErrorMessage = null;
                            _context.Payments.Add(payment);
                            await _context.SaveChangesAsync();
                            return true;
                        }
                        else
                        {
                            payment.ErrorMessage = $"Default: {errorMessage}; Fallback: {fallbackError}";
                        }
                    }

                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment {CorrelationId}", request.CorrelationId);
                payment.ErrorMessage = ex.Message;
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();
                return false;
            }
        }

        public async Task<PaymentSummaryResponse> GetPaymentsSummaryAsync(DateTime? from, DateTime? to)
        {
            var query = _context.Payments.Where(p => p.Success);

            if (from.HasValue)
                query = query.Where(p => p.ProcessedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(p => p.ProcessedAt <= to.Value);

            var summary = await query
                .GroupBy(p => p.ProcessorType)
                .Select(g => new
                {
                    ProcessorType = g.Key,
                    TotalRequests = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .ToListAsync();

            var response = new PaymentSummaryResponse();

            foreach (var item in summary)
            {
                var data = new PaymentSummaryData
                {
                    TotalRequests = item.TotalRequests,
                    TotalAmount = item.TotalAmount
                };

                if (item.ProcessorType == "default")
                    response.Default = data;
                else if (item.ProcessorType == "fallback")
                    response.Fallback = data;
            }

            return response;
        }
    }
}
