using System.ComponentModel.DataAnnotations;

namespace FlightBooking.DTOs
{
    // Meal DTOs
    public class MealDto
    {
        public int MealId { get; set; }
        public string MealName { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? MealType { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateMealDto
    {
        [Required]
        [StringLength(100)]
        public string MealName { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [StringLength(50)]
        public string? MealType { get; set; }

        [StringLength(255)]
        public string? ImageUrl { get; set; }
    }

    public class UpdateMealDto
    {
        [StringLength(100)]
        public string? MealName { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Price { get; set; }

        [StringLength(50)]
        public string? MealType { get; set; }

        [StringLength(255)]
        public string? ImageUrl { get; set; }

        public bool? IsActive { get; set; }
    }

    // Luggage DTOs
    public class LuggageDto
    {
        public int LuggageId { get; set; }
        public string LuggageName { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal WeightLimit { get; set; }
        public string? LuggageType { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateLuggageDto
    {
        [Required]
        [StringLength(100)]
        public string LuggageName { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal WeightLimit { get; set; }

        [StringLength(50)]
        public string? LuggageType { get; set; }

        [StringLength(255)]
        public string? ImageUrl { get; set; }
    }

    public class UpdateLuggageDto
    {
        [StringLength(100)]
        public string? LuggageName { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Price { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? WeightLimit { get; set; }

        [StringLength(50)]
        public string? LuggageType { get; set; }

        [StringLength(255)]
        public string? ImageUrl { get; set; }

        public bool? IsActive { get; set; }
    }

    // Insurance DTOs
    public class InsuranceDto
    {
        public int InsuranceId { get; set; }
        public string InsuranceName { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal? CoverageAmount { get; set; }
        public string? InsuranceType { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateInsuranceDto
    {
        [Required]
        [StringLength(100)]
        public string InsuranceName { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? CoverageAmount { get; set; }

        [StringLength(50)]
        public string? InsuranceType { get; set; }

        [StringLength(255)]
        public string? ImageUrl { get; set; }
    }

    public class UpdateInsuranceDto
    {
        [StringLength(100)]
        public string? InsuranceName { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Price { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? CoverageAmount { get; set; }

        [StringLength(50)]
        public string? InsuranceType { get; set; }

        [StringLength(255)]
        public string? ImageUrl { get; set; }

        public bool? IsActive { get; set; }
    }

    // Booking Service DTOs
    public class BookingServiceDto
    {
        public int BookingServiceId { get; set; }
        public int BookingId { get; set; }
        public string ServiceType { get; set; } = null!;
        public MealDto? Meal { get; set; }
        public LuggageDto? Luggage { get; set; }
        public InsuranceDto? Insurance { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class AddServiceToBookingDto
    {
        [Required]
        public int BookingId { get; set; }

        [Required]
        [StringLength(50)]
        public string ServiceType { get; set; } = null!; // MEAL, LUGGAGE, INSURANCE

        public int? MealId { get; set; }
        public int? LuggageId { get; set; }
        public int? InsuranceId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;
    }
}


