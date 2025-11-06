using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlightBooking.Models;

public partial class BookingService
{
    [Key]
    public int BookingServiceId { get; set; }

    [Required]
    public int BookingId { get; set; }

    [StringLength(50)]
    public string ServiceType { get; set; } = null!; // MEAL, LUGGAGE, INSURANCE

    public int? MealId { get; set; }

    public int? LuggageId { get; set; }

    public int? InsuranceId { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    public int Quantity { get; set; } = 1;

    public DateTime? CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("BookingId")]
    public virtual Booking Booking { get; set; } = null!;

    [ForeignKey("MealId")]
    public virtual Meal? Meal { get; set; }

    [ForeignKey("LuggageId")]
    public virtual Luggage? Luggage { get; set; }

    [ForeignKey("InsuranceId")]
    public virtual Insurance? Insurance { get; set; }
}


