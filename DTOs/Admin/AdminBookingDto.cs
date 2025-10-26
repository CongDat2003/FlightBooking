namespace FlightBooking.DTOs.Admin
{
    public class AdminBookingResponseDto
    {
        public int BookingId { get; set; }
        public string BookingReference { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string FlightNumber { get; set; }
        public string Route { get; set; }
        public DateTime FlightDate { get; set; }
        public string BookingStatus { get; set; }
        public string PaymentStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime BookingDate { get; set; }
        public int PassengerCount { get; set; }
        public List<AdminBookingSeatDto> Seats { get; set; } = new List<AdminBookingSeatDto>();
    }

    public class AdminBookingSeatDto
    {
        public string SeatNumber { get; set; }
        public string SeatClass { get; set; }
        public string PassengerName { get; set; }
        public string? PassengerIdNumber { get; set; }
        public decimal SeatPrice { get; set; }
    }

    public class UpdateBookingStatusDto
    {
        public string BookingStatus { get; set; }
        public string? PaymentStatus { get; set; }
        public string? Notes { get; set; }
    }
}
