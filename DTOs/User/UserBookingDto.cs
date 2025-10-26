namespace FlightBooking.DTOs.User
{
    public class UserBookingHistoryDto
    {
        public int BookingId { get; set; }
        public string BookingReference { get; set; }
        public string FlightNumber { get; set; }
        public string AirlineName { get; set; }
        public string Route { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public string BookingStatus { get; set; }
        public string PaymentStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime BookingDate { get; set; }
        public int PassengerCount { get; set; }
        public bool CanCancel { get; set; }
    }

    public class BookingDetailDto
    {
        public int BookingId { get; set; }
        public string BookingReference { get; set; }
        public FlightDetailDto Flight { get; set; }
        public string BookingStatus { get; set; }
        public string PaymentStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime BookingDate { get; set; }
        public string? Notes { get; set; }
        public List<PassengerSeatDto> Passengers { get; set; } = new List<PassengerSeatDto>();
    }

    public class FlightDetailDto
    {
        public string FlightNumber { get; set; }
        public string AirlineName { get; set; }
        public string AircraftModel { get; set; }
        public string DepartureAirport { get; set; }
        public string ArrivalAirport { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public string? Gate { get; set; }
    }

    public class PassengerSeatDto
    {
        public string SeatNumber { get; set; }
        public string SeatClass { get; set; }
        public string PassengerName { get; set; }
        public decimal SeatPrice { get; set; }
        public bool IsWindow { get; set; }
        public bool IsAisle { get; set; }
    }
}
