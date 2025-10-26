using System.ComponentModel.DataAnnotations;

namespace FlightBooking.DTOs.Admin
{
    public class CreateAirportDto
    {
        [Required]
        [StringLength(10)]
        public string AirportCode { get; set; }

        [Required]
        [StringLength(100)]
        public string AirportName { get; set; }

        [Required]
        [StringLength(50)]
        public string City { get; set; }

        [Required]
        [StringLength(50)]
        public string Country { get; set; }

        [StringLength(50)]
        public string? Timezone { get; set; }
    }

    public class UpdateAirportDto
    {
        [StringLength(10)]
        public string? AirportCode { get; set; }

        [StringLength(100)]
        public string? AirportName { get; set; }

        [StringLength(50)]
        public string? City { get; set; }

        [StringLength(50)]
        public string? Country { get; set; }

        [StringLength(50)]
        public string? Timezone { get; set; }
    }
}
