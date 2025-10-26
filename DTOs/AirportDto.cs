namespace FlightBooking.DTOs
{
    public class AirportDto
    {
        public int AirportId { get; set; }
        public string AirportCode { get; set; }
        public string AirportName { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string? Timezone { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<FlightSummaryDto> DepartureFlights { get; set; } = new List<FlightSummaryDto>();
        public List<FlightSummaryDto> ArrivalFlights { get; set; } = new List<FlightSummaryDto>();
        public int TotalDepartureFlights { get; set; }
        public int TotalArrivalFlights { get; set; }
    }
}
