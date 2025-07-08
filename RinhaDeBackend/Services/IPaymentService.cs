using RinhaDeBackend.Models;

namespace RinhaDeBackend.Services
{
    public interface IPaymentService
    {
        Task<bool> ProcessPaymentAsync(PaymentRequest request);
        Task<PaymentsSummaryResponse> GetPaymentsSummaryAsync(DateTime? from, DateTime? to);
    }
}
