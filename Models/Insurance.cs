using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightBooking.Models;

public partial class Insurance
{
    [Key]
    public int InsuranceId { get; set; }

    [Required]
    [StringLength(100)]
    public string InsuranceName { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? CoverageAmount { get; set; } // Coverage in VND

    [StringLength(50)]
    public string? InsuranceType { get; set; } // BASIC, PREMIUM, DELUXE, etc.

    [StringLength(255)]
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }
}


