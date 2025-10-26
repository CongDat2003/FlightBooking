using FlightBooking.DTOs.User;

namespace FlightBooking.Services
{
    public interface IUserService
    {
        Task<UserProfileDto> RegisterAsync(RegisterUserDto registerDto);
        Task<UserProfileDto> LoginAsync(LoginDto loginDto);
        Task<UserProfileDto> GetProfileAsync(int userId);
        Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileDto updateDto);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto passwordDto);
        Task<List<UserBookingHistoryDto>> GetBookingHistoryAsync(int userId, int page = 1, int pageSize = 10);
        Task<BookingDetailDto> GetBookingDetailAsync(int userId, int bookingId);
        Task<bool> CancelBookingAsync(int userId, int bookingId);
        Task<bool> DeleteAccountAsync(int userId, string password);
        Task SendPasswordResetOtpAsync(string email);
        Task<bool> VerifyPasswordResetOtpAsync(string email, string otpCode);
        Task<bool> ResetPasswordAsync(string email, string otpCode, string newPassword);
    }
}
