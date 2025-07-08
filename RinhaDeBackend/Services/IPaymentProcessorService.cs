using RinhaDeBackend.Models;

namespace RinhaDeBackend.Services
{
    public interface IPaymentProcessorService
    {
        Task<bool> ProcessPaymentAsync(PaymentProcessorRequest request, string processorType);
        Task<ServiceHealthResponse?> GetServiceHealthAsync(string processorType);
    }
}
