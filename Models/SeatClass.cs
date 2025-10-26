using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightBooking.Models;

public partial class SeatClass
{
    [Key]
    public int ClassId { get; set; }

    [Required]
    [StringLength(20)]
    public string ClassName { get; set; } = null!;

    [StringLength(100)]
    public string? ClassDescription { get; set; }

    [Column(TypeName = "decimal(3,2)")]
    public decimal? PriceMultiplier { get; set; } = 1.0m;

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
