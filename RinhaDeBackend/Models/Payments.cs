using System.ComponentModel.DataAnnotations;

namespace RinhaDeBackend.Models
{
    public class Payment
    {
        public int Id { get; set; }

        [Required]
        public Guid CorrelationId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime RequestedAt { get; set; }

        [Required]
        public DateTime ProcessedAt { get; set; }

        [Required]
        public string ProcessorType { get; set; } = string.Empty; // "default" or "fallback"

        [Required]
        public bool Success { get; set; }

        public string? ErrorMessage { get; set; }
    }

    public class PaymentRequest
    {
        [Required]
        public Guid CorrelationId { get; set; }

        [Required]
        public decimal Amount { get; set; }
    }

    public class PaymentSummaryResponse
    {
        public PaymentSummaryData Default { get; set; } = new();
        public PaymentSummaryData Fallback { get; set; } = new();
    }

    public class PaymentSummaryData
    {
        public int TotalRequests { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ProcessorHealthResponse
    {
        public bool Failing { get; set; }
        public int MinResponseTime { get; set; }
    }

    public class ProcessorPaymentRequest
    {
        public Guid CorrelationId { get; set; }
        public decimal Amount { get; set; }
        public DateTime RequestedAt { get; set; }
    }

    public class ProcessorPaymentResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}
