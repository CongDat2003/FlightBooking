using System.ComponentModel.DataAnnotations;

namespace FlightBooking.DTOs.Admin
{
    public class AdminUserResponseDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string? Phone { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalSpent { get; set; }
    }

    public class UpdateUserStatusDto
    {
        public bool IsActive { get; set; }
    }

    public class UpdateUserDto
    {
        [StringLength(50)]
        public string? Username { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(255)]
        public string? Password { get; set; }

        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        public bool? IsActive { get; set; }
    }
}
