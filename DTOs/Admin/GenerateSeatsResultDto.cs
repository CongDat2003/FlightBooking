namespace FlightBooking.DTOs.Admin
{
    public class GenerateSeatsResultDto
    {
        public int TotalFlightsProcessed { get; set; }
        public int TotalSeatsCreated { get; set; }
        public int SuccessfulFlights { get; set; }
        public int FailedFlights { get; set; }
        public List<FlightSeatsInfoDto> FlightDetails { get; set; } = new List<FlightSeatsInfoDto>();
    }

    public class FlightSeatsInfoDto
    {
        public int FlightId { get; set; }
        public string FlightNumber { get; set; }
        public int SeatsCreated { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}










