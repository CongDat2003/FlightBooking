using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace FlightBooking.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        public int BookingId { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } // VNPAY, MOMO, ZALOPAY, CARD

        [Required]
        [StringLength(100)]
        public string TransactionId { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "PENDING"; // PENDING, SUCCESS, FAILED, REFUNDED

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ProcessedAt { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [StringLength(1000)]
        public string? PaymentData { get; set; } // JSON data from payment gateway

        // Navigation properties
        [ForeignKey("BookingId")]
        public virtual Booking Booking { get; set; }
    }
}
