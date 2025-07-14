using RinhaDeBackend.Models;

namespace RinhaDeBackend.Service
{
    public interface IPaymentService
    {
        Task<bool> ProcessPaymentAsync(PaymentRequest request);
        Task<PaymentSummaryResponse> GetPaymentsSummaryAsync(DateTime? from, DateTime? to);
    }
}
