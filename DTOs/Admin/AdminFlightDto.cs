namespace FlightBooking.DTOs.Admin
{
    public class CreateFlightDto
    {
        public string FlightNumber { get; set; }
        public int AirlineId { get; set; }
        public int AircraftTypeId { get; set; }
        public int DepartureAirportId { get; set; }
        public int ArrivalAirportId { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public decimal BasePrice { get; set; }
        public string? Gate { get; set; }
    }

    public class UpdateFlightDto
    {
        public string? FlightNumber { get; set; }
        public DateTime? DepartureTime { get; set; }
        public DateTime? ArrivalTime { get; set; }
        public decimal? BasePrice { get; set; }
        public string? Status { get; set; }
        public string? Gate { get; set; }
    }

    public class AdminFlightResponseDto
    {
        public int FlightId { get; set; }
        public string FlightNumber { get; set; }
        public string AirlineName { get; set; }
        public string AircraftModel { get; set; }
        public string DepartureAirport { get; set; }
        public string ArrivalAirport { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public decimal BasePrice { get; set; }
        public string Status { get; set; }
        public string? Gate { get; set; }
        public int TotalSeats { get; set; }
        public int BookedSeats { get; set; }
        public int AvailableSeats { get; set; }
        public decimal Revenue { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
