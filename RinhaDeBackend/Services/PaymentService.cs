using Microsoft.EntityFrameworkCore;
using RinhaDeBackend.Data;
using RinhaDeBackend.Models;

namespace RinhaDeBackend.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly PaymentContext _context;
        private readonly IPaymentProcessorService _processorService;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
                PaymentContext context,
                IPaymentProcessorService processorService,
                ILogger<PaymentService> logger)
        {
            _context = context;
            _processorService = processorService;
            _logger = logger;
        }

        public async Task<PaymentsSummaryResponse> GetPaymentsSummaryAsync(DateTime? from, DateTime? to)
        {
            var query = _context.Payments.AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(p => p.RequestedAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(p => p.RequestedAt <= to.Value);
            }

            var defaultPayments = await query
                .Where(p => p.ProcessorType == "default")
                .GroupBy(p => 1)
                .Select(g => new ProcessorSummary
                {
                    TotalRequests = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .FirstOrDefaultAsync() ?? new ProcessorSummary();

            var fallbackPayments = await query
                .Where(p => p.ProcessorType == "fallback")
                .GroupBy(p => 1)
                .Select(g => new ProcessorSummary
                {
                    TotalRequests = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .FirstOrDefaultAsync() ?? new ProcessorSummary();

            return new PaymentsSummaryResponse
            {
                Default = defaultPayments,
                Fallback = fallbackPayments
            };
        }


        public async Task<bool> ProcessPaymentAsync(PaymentRequest request)
        {
            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.CorrelationId == request.CorrelationId);

            if (existingPayment != null)
            {
                _logger.LogWarning($"Payment with correlationId {request.CorrelationId} already exists");
                return false;
            }

            var requestedAt = DateTime.UtcNow;
            var processorRequest = new PaymentProcessorRequest
            {
                CorrelationId = request.CorrelationId,
                Amount = request.Amount,
                RequestedAt = requestedAt
            };

            var success = await TryProcessWithProcessor(processorRequest, "default");
            var processorUsed = "default";

            if (!success)
            {
                _logger.LogInformation($"Default processor failed for {request.CorrelationId}, trying fallback");
                success = await TryProcessWithProcessor(processorRequest, "fallback");
                processorUsed = "fallback";
            }

            if (success)
            {
                var payment = new Payment
                {
                    CorrelationId = request.CorrelationId,
                    Amount = request.Amount,
                    RequestedAt = requestedAt,
                    ProcessorType = processorUsed
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Payment {request.CorrelationId} processed successfully via {processorUsed}");
            }
            else
            {
                _logger.LogError($"Failed to process payment {request.CorrelationId} with both processors");
            }

            return success;
        }


        private async Task<bool> TryProcessWithProcessor(PaymentProcessorRequest request, string processorType)
        {
            try
            {
                var health = await _processorService.GetServiceHealthAsync(processorType);

                if (health?.Failing == true)
                {
                    _logger.LogWarning($"Processor {processorType} is failing, skipping");
                    return false;
                }
                return await _processorService.ProcessPaymentAsync(request, processorType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing payment with {processorType} processor");
                return false;
            }
        }

    }
}
