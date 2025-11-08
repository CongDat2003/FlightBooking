using FlightBooking.DTOs;
using FlightBooking.DTOs.Admin;
using FlightBooking.DTOs.User;

namespace FlightBooking.Services
{
    public interface IAdminService
    {
        // Dashboard
        Task<DashboardStatsDto> GetDashboardStatsAsync();

        // Flight Management
        Task<List<AdminFlightResponseDto>> GetAllFlightsAsync(int page = 1, int pageSize = 10);
        Task<AdminFlightResponseDto> GetFlightByIdAsync(int flightId);
        Task<AdminFlightResponseDto> CreateFlightAsync(CreateFlightDto flightDto);
        Task<AdminFlightResponseDto> UpdateFlightAsync(int flightId, UpdateFlightDto flightDto);
        Task<bool> DeleteFlightAsync(int flightId);
        Task<bool> GenerateSeatsForFlightAsync(int flightId, bool forceRegenerate = false);
        Task<GenerateSeatsResultDto> GenerateSeatsForAllFlightsWithoutSeatsAsync();

        // Booking Management
        Task<AdminBookingResponseDto> CreateBookingAsync(CreateBookingDto bookingDto);
        Task<List<AdminBookingResponseDto>> GetAllBookingsAsync(int page = 1, int pageSize = 10);
        Task<AdminBookingResponseDto> GetBookingByIdAsync(int bookingId);
        Task<AdminBookingResponseDto> UpdateBookingStatusAsync(int bookingId, UpdateBookingStatusDto statusDto);
        Task<bool> CancelBookingAsync(int bookingId);
        Task<bool> DeleteBookingPermanentlyAsync(int bookingId);
        Task<bool> ApproveRestoreAsync(int bookingId, string? note = null);
        Task<bool> RejectRestoreAsync(int bookingId, string? note = null);

        // User Management
        Task<List<AdminUserResponseDto>> GetAllUsersAsync(int page = 1, int pageSize = 10);
        Task<AdminUserResponseDto> GetUserByIdAsync(int userId);
        Task<AdminUserResponseDto> CreateUserAsync(RegisterUserDto registerDto);
        Task<AdminUserResponseDto> UpdateUserAsync(int userId, UpdateUserDto updateDto);
        Task<AdminUserResponseDto> UpdateUserStatusAsync(int userId, UpdateUserStatusDto statusDto);
        Task<bool> DeleteUserAsync(int userId);

        // Reports
        Task<List<RevenueByMonthDto>> GetRevenueReportAsync(int year);
        Task<List<RevenueByMonthDto>> GetRevenueReportAsync(int startYear, int endYear);
        Task<List<PopularRouteDto>> GetPopularRoutesAsync(int topCount = 10);
        Task<List<AirlineStatsDto>> GetAirlineStatsAsync();
    }
}
