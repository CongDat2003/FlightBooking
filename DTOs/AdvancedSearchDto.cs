namespace FlightBooking.DTOs
{
    public class AdvancedFlightSearchDto
    {
        public string DepartureAirportCode { get; set; }
        public string ArrivalAirportCode { get; set; }
        public DateTime DepartureDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public int Passengers { get; set; } = 1;
        public string? SeatClass { get; set; }
        public List<string>? Airlines { get; set; }
        public TimeSpan? DepartureTimeFrom { get; set; }
        public TimeSpan? DepartureTimeTo { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MaxStops { get; set; } = 0;
        public string SortBy { get; set; } = "PRICE"; // PRICE, DURATION, DEPARTURE_TIME
        public string SortOrder { get; set; } = "ASC"; // ASC, DESC
    }

    public class FlightSearchResultDto
    {
        public List<FlightResponseDto> OutboundFlights { get; set; } = new List<FlightResponseDto>();
        public List<FlightResponseDto>? ReturnFlights { get; set; }
        public SearchMetadataDto Metadata { get; set; }
    }

    public class SearchMetadataDto
    {
        public int TotalResults { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public List<string> AvailableAirlines { get; set; } = new List<string>();
        public List<TimeSlotDto> DepartureTimeSlots { get; set; } = new List<TimeSlotDto>();
    }

    public class TimeSlotDto
    {
        public string Label { get; set; }
        public TimeSpan From { get; set; }
        public TimeSpan To { get; set; }
        public int FlightCount { get; set; }
    }

    public class PriceTrendDto
    {
        public string Route { get; set; }
        public List<PricePointDto> PriceHistory { get; set; } = new List<PricePointDto>();
        public string Recommendation { get; set; }
    }

    public class PricePointDto
    {
        public DateTime Date { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public decimal AvgPrice { get; set; }
        public int FlightCount { get; set; }
    }
}
