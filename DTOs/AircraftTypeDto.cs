namespace FlightBooking.DTOs
{
    public class AircraftTypeDto
    {
        public int AircraftTypeId { get; set; }
        public string AircraftModel { get; set; }
        public string Manufacturer { get; set; }
        public int TotalSeats { get; set; }
        public int EconomySeats { get; set; }
        public int BusinessSeats { get; set; }
        public int FirstClassSeats { get; set; }
        public string SeatMapLayout { get; set; }
        public List<FlightSummaryDto> Flights { get; set; } = new List<FlightSummaryDto>();
    }

    public class FlightSummaryDto
    {
        public int FlightId { get; set; }
        public string FlightNumber { get; set; }
        public string AirlineName { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public string Status { get; set; }
        public decimal BasePrice { get; set; }
        public string? DepartureAirport { get; set; }
        public string? ArrivalAirport { get; set; }
    }
}
