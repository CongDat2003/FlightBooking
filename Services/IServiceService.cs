using FlightBooking.DTOs;

namespace FlightBooking.Services
{
    public interface IServiceService
    {
        // Meal operations
        Task<List<MealDto>> GetAllMealsAsync();
        Task<MealDto?> GetMealByIdAsync(int mealId);
        Task<MealDto> CreateMealAsync(CreateMealDto createDto);
        Task<bool> UpdateMealAsync(int mealId, UpdateMealDto updateDto);
        Task<bool> DeleteMealAsync(int mealId);

        // Luggage operations
        Task<List<LuggageDto>> GetAllLuggagesAsync();
        Task<LuggageDto?> GetLuggageByIdAsync(int luggageId);
        Task<LuggageDto> CreateLuggageAsync(CreateLuggageDto createDto);
        Task<bool> UpdateLuggageAsync(int luggageId, UpdateLuggageDto updateDto);
        Task<bool> DeleteLuggageAsync(int luggageId);

        // Insurance operations
        Task<List<InsuranceDto>> GetAllInsurancesAsync();
        Task<InsuranceDto?> GetInsuranceByIdAsync(int insuranceId);
        Task<InsuranceDto> CreateInsuranceAsync(CreateInsuranceDto createDto);
        Task<bool> UpdateInsuranceAsync(int insuranceId, UpdateInsuranceDto updateDto);
        Task<bool> DeleteInsuranceAsync(int insuranceId);

        // Booking service operations
        Task<List<BookingServiceDto>> GetBookingServicesAsync(int bookingId);
        Task<BookingServiceDto> AddServiceToBookingAsync(AddServiceToBookingDto addDto);
        Task<bool> RemoveServiceFromBookingAsync(int bookingServiceId);
        Task<decimal> CalculateBookingServicesTotalAsync(int bookingId);
    }
}


