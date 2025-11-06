using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightBooking.Models;

public partial class Meal
{
    [Key]
    public int MealId { get; set; }

    [Required]
    [StringLength(100)]
    public string MealName { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [StringLength(50)]
    public string? MealType { get; set; } // VEGETARIAN, VEGAN, HALAL, KOSHER, REGULAR, etc.

    [StringLength(255)]
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }
}


