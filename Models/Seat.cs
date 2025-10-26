using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightBooking.Models;

public partial class Seat
{
    [Key]
    public int SeatId { get; set; }

    public int FlightId { get; set; }

    [Required]
    [StringLength(10)]
    public string SeatNumber { get; set; } = null!;

    public int SeatRow { get; set; }

    [Required]
    [StringLength(2)]
    public string SeatColumn { get; set; } = null!;

    public int ClassId { get; set; }

    public bool? IsWindow { get; set; }

    public bool? IsAisle { get; set; }

    public bool? IsEmergencyExit { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? ExtraFee { get; set; } = 0m;

    public bool? IsAvailable { get; set; }

    public DateTime? CreatedAt { get; set; } = DateTime.Now;

    public virtual ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();

    [ForeignKey("ClassId")]
    public virtual SeatClass Class { get; set; } = null!;

    [ForeignKey("FlightId")]
    public virtual Flight Flight { get; set; } = null!;
}
