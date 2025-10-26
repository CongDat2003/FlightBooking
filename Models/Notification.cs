using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace FlightBooking.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        [StringLength(500)]
        public string Message { get; set; }

        [StringLength(20)]
        public string Type { get; set; } // BOOKING, FLIGHT_UPDATE, PAYMENT, REMINDER

        [StringLength(20)]
        public string Status { get; set; } = "UNREAD"; // UNREAD, READ

        public int? RelatedBookingId { get; set; }
        public int? RelatedFlightId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ReadAt { get; set; }

        [StringLength(1000)]
        public string? AdditionalData { get; set; } // JSON data

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("RelatedBookingId")]
        public virtual Booking? RelatedBooking { get; set; }

        [ForeignKey("RelatedFlightId")]
        public virtual Flight? RelatedFlight { get; set; }
    }
}
