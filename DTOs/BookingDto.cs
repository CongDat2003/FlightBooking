using System.ComponentModel.DataAnnotations;

namespace FlightBooking.DTOs
{
    public class CreateBookingDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int FlightId { get; set; }

        [Required]
        public int SeatClassId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "At least one passenger is required.")]
        public int Passengers { get; set; } = 1;

        [Required]
        [MinLength(1, ErrorMessage = "Passenger details must be provided for each passenger.")]
        public List<PassengerInfoDto> PassengerDetails { get; set; } = new List<PassengerInfoDto>();

        public string? Notes { get; set; }
    }

    public class BookingSeatDto
    {
        public int SeatId { get; set; }
        public string PassengerName { get; set; }
        public string? PassengerIdNumber { get; set; }
    }

    public class PassengerInfoDto
    {
        [Required]
        [StringLength(100, ErrorMessage = "Passenger name cannot exceed 100 characters.")]
        public string PassengerName { get; set; }

        [StringLength(50, ErrorMessage = "Passenger ID number cannot exceed 50 characters.")]
        public string? PassengerIdNumber { get; set; }
    }

    public class BookingResponseDto
    {
        public int BookingId { get; set; }
        public string BookingReference { get; set; }
        public string BookingStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime BookingDate { get; set; }
        public int UserId { get; set; }
        public FlightResponseDto Flight { get; set; }
        public List<BookedSeatDto> Seats { get; set; } = new List<BookedSeatDto>();
    }

    public class BookedSeatDto
    {
        public string SeatNumber { get; set; }
        public string SeatClassName { get; set; }
        public string PassengerName { get; set; }
        public decimal SeatPrice { get; set; }
    }
}
