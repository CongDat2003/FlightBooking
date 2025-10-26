using System.ComponentModel.DataAnnotations;

namespace FlightBooking.Models
{
    public class PasswordResetToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Email { get; set; } = null!;

        [Required]
        [StringLength(6)]
        public string OtpCode { get; set; } = null!;

        public DateTime ExpirationTime { get; set; }

        public bool IsUsed { get; set; } = false;
    }
}
