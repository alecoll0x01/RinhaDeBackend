using System.ComponentModel.DataAnnotations;

namespace RinhaDeBackend.Models
{
    public class PaymentProcessorRequest
    {
        [Required]
        public Guid CorrelationId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime RequestedAt { get; set; }
    }
}
