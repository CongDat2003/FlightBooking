using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FlightBooking.Models;

public partial class AircraftType
{
    [Key]
    public int AircraftTypeId { get; set; }

    [Required]
    [StringLength(50)]
    public string AircraftModel { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string Manufacturer { get; set; } = null!;

    public int TotalSeats { get; set; }

    public int EconomySeats { get; set; }

    public int BusinessSeats { get; set; }

    public int FirstClassSeats { get; set; }

    [Required]
    [StringLength(20)]
    public string SeatMapLayout { get; set; } = null!;

    public virtual ICollection<Flight> Flights { get; set; } = new List<Flight>();
}
