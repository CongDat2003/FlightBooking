using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightBooking.Models;

public partial class BookingSeat
{
    [Key]
    public int BookingSeatId { get; set; }

    public int BookingId { get; set; }

    public int SeatId { get; set; }

    [Required]
    [StringLength(100)]
    public string PassengerName { get; set; } = null!;

    [StringLength(50)]
    public string? PassengerIdNumber { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal SeatPrice { get; set; }

    public DateTime? CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("BookingId")]
    public virtual Booking Booking { get; set; } = null!;

    [ForeignKey("SeatId")]
    public virtual Seat Seat { get; set; } = null!;
}
