using System.ComponentModel.DataAnnotations;

namespace FlightBooking.DTOs.Admin
{
    public class CreateAircraftTypeDto
    {
        [Required]
        [StringLength(50)]
        public string AircraftModel { get; set; }

        [Required]
        [StringLength(50)]
        public string Manufacturer { get; set; }

        [Range(1, 1000)]
        public int TotalSeats { get; set; }

        [Range(0, 1000)]
        public int EconomySeats { get; set; }

        [Range(0, 200)]
        public int BusinessSeats { get; set; }

        [Range(0, 50)]
        public int FirstClassSeats { get; set; }

        [Required]
        [StringLength(20)]
        public string SeatMapLayout { get; set; }
    }

    public class UpdateAircraftTypeDto
    {
        [StringLength(50)]
        public string? AircraftModel { get; set; }

        [StringLength(50)]
        public string? Manufacturer { get; set; }

        [Range(1, 1000)]
        public int? TotalSeats { get; set; }

        [Range(0, 1000)]
        public int? EconomySeats { get; set; }

        [Range(0, 200)]
        public int? BusinessSeats { get; set; }

        [Range(0, 50)]
        public int? FirstClassSeats { get; set; }

        [StringLength(20)]
        public string? SeatMapLayout { get; set; }
    }
}
