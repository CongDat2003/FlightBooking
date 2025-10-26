using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FlightBooking.Models;

public partial class Airport
{
    [Key]
    public int AirportId { get; set; }

    [Required]
    [StringLength(10)]
    public string AirportCode { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string AirportName { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string City { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string Country { get; set; } = null!;

    [StringLength(50)]
    public string? Timezone { get; set; }

    public DateTime? CreatedAt { get; set; } = DateTime.Now;

    public virtual ICollection<Flight> FlightArrivalAirports { get; set; } = new List<Flight>();

    public virtual ICollection<Flight> FlightDepartureAirports { get; set; } = new List<Flight>();
}
