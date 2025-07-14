using RinhaDeBackend.Models;

namespace RinhaDeBackend.Service
{
    public interface IPaymentProcessorService
    {
        Task<(bool success, string? errorMessage)> ProcessPaymentAsync(Guid correlationId, decimal amount, string processorType);
        Task<ProcessorHealthResponse?> GetHealthAsync(string processorType);
    }
}
