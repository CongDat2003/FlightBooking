using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FlightBooking.Models;

public partial class Airline
{
    [Key]
    public int AirlineId { get; set; }

    [Required]
    [StringLength(10)]
    public string AirlineCode { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string AirlineName { get; set; } = null!;

    [StringLength(255)]
    public string? LogoUrl { get; set; }

    public DateTime? CreatedAt { get; set; } = DateTime.Now;

    public virtual ICollection<Flight> Flights { get; set; } = new List<Flight>();
}
