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
        public string ProcessorType { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
