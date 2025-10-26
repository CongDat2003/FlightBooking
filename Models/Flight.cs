using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightBooking.Models;

public partial class Flight
{
    [Key]
    public int FlightId { get; set; }

    [Required]
    [StringLength(20)]
    public string FlightNumber { get; set; } = null!;

    public int AirlineId { get; set; }

    public int AircraftTypeId { get; set; }

    public int DepartureAirportId { get; set; }

    public int ArrivalAirportId { get; set; }

    public DateTime DepartureTime { get; set; }

    public DateTime ArrivalTime { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal BasePrice { get; set; }

    [StringLength(20)]
    public string? Status { get; set; } = "SCHEDULED";

    [StringLength(10)]
    public string? Gate { get; set; }

    public DateTime? CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("AircraftTypeId")]
    public virtual AircraftType AircraftType { get; set; } = null!;

    [ForeignKey("AirlineId")]
    public virtual Airline Airline { get; set; } = null!;

    [ForeignKey("ArrivalAirportId")]
    public virtual Airport ArrivalAirport { get; set; } = null!;

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    [ForeignKey("DepartureAirportId")]
    public virtual Airport DepartureAirport { get; set; } = null!;

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
