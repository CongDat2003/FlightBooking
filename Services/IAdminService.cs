using FlightBooking.DTOs.Admin;

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
        Task<bool> GenerateSeatsForFlightAsync(int flightId);

        // Booking Management
        Task<List<AdminBookingResponseDto>> GetAllBookingsAsync(int page = 1, int pageSize = 10);
        Task<AdminBookingResponseDto> GetBookingByIdAsync(int bookingId);
        Task<AdminBookingResponseDto> UpdateBookingStatusAsync(int bookingId, UpdateBookingStatusDto statusDto);
        Task<bool> CancelBookingAsync(int bookingId);

        // User Management
        Task<List<AdminUserResponseDto>> GetAllUsersAsync(int page = 1, int pageSize = 10);
        Task<AdminUserResponseDto> GetUserByIdAsync(int userId);
        Task<AdminUserResponseDto> UpdateUserStatusAsync(int userId, UpdateUserStatusDto statusDto);

        // Reports
        Task<List<RevenueByMonthDto>> GetRevenueReportAsync(int year);
        Task<List<RevenueByMonthDto>> GetRevenueReportAsync(int startYear, int endYear);
        Task<List<PopularRouteDto>> GetPopularRoutesAsync(int topCount = 10);
    }
}
