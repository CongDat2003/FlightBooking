namespace FlightBooking.DTOs
{
    public class FlightSearchDto
    {
        public string DepartureAirportCode { get; set; }
        public string ArrivalAirportCode { get; set; }
        public DateTime DepartureDate { get; set; }
        public int? Passengers { get; set; } = 1;
        public string? SeatClass { get; set; }
    }

    public class FlightResponseDto
    {
        public int FlightId { get; set; }
        public string FlightNumber { get; set; }
        public string AirlineName { get; set; }
        public string AirlineCode { get; set; }
        public string DepartureAirport { get; set; }
        public string ArrivalAirport { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public decimal BasePrice { get; set; }
        public string Status { get; set; }
        public string? Gate { get; set; }
        public int AvailableSeats { get; set; }
    }
}
