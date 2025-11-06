using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightBooking.Models;

public partial class Luggage
{
    [Key]
    public int LuggageId { get; set; }

    [Required]
    [StringLength(100)]
    public string LuggageName { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal WeightLimit { get; set; } // Weight in kg

    [StringLength(50)]
    public string? LuggageType { get; set; } // HAND, CHECKED, OVERWEIGHT, etc.

    [StringLength(255)]
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }
}


