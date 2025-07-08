using System.ComponentModel.DataAnnotations;

namespace RinhaDeBackend.Models
{
    public class PaymentRequest
    {
        [Required]
        public Guid CorrelationId { get; set; }

        [Required]
        public decimal Amount { get; set; }
    }
}
