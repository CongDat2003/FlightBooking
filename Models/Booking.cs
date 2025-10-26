using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightBooking.Models;

public partial class Booking
{
    [Key]
    public int BookingId { get; set; }

    [Required]
    [StringLength(20)]
    public string BookingReference { get; set; } = null!;

    public int UserId { get; set; }

    public int FlightId { get; set; }

    [StringLength(20)]
    public string? BookingStatus { get; set; } = "CONFIRMED";

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    [StringLength(20)]
    public string? PaymentStatus { get; set; } = "PENDING";

    public DateTime? BookingDate { get; set; } = DateTime.Now;

    public string? Notes { get; set; }

    public virtual ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();

    [ForeignKey("FlightId")]
    public virtual Flight Flight { get; set; } = null!;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}
