using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace FlightBooking.Models
{
    [Table("Messages")]
    public class Message
    {
        [Key]
        public int MessageId { get; set; }

        public int? UserId { get; set; } // null nếu là tin nhắn từ admin/system

        [Required]
        [StringLength(2000)]
        public string Content { get; set; }

        [StringLength(20)]
        public string SenderType { get; set; } = "USER"; // USER, ADMIN, SYSTEM

        public bool IsRead { get; set; } = false; // false = chưa đọc, true = đã đọc

        public bool IsAutoReply { get; set; } = false; // true nếu là auto-reply

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ReadAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}





































