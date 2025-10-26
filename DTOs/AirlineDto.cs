using System.ComponentModel.DataAnnotations;

namespace FlightBooking.DTOs
{
    public class AirlineDto
    {
        public int AirlineId { get; set; }
        public string AirlineCode { get; set; }
        public string AirlineName { get; set; }
        public string? LogoUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<FlightSummaryDto> Flights { get; set; } = new List<FlightSummaryDto>();
    }

    public class CreateAirlineDto
    {
        [Required]
        [StringLength(10)]
        public string AirlineCode { get; set; }

        [Required]
        [StringLength(100)]
        public string AirlineName { get; set; }

        [StringLength(255)]
        public string? LogoUrl { get; set; }
    }

    public class UpdateAirlineDto
    {
        [StringLength(10)]
        public string? AirlineCode { get; set; }

        [StringLength(100)]
        public string? AirlineName { get; set; }

        [StringLength(255)]
        public string? LogoUrl { get; set; }
    }
}
