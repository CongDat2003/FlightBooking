using FlightBooking.DTOs;

namespace FlightBooking.Services
{
    public interface INotificationService
    {
        Task SendBookingConfirmationAsync(int bookingId);
        Task SendFlightReminderAsync(int bookingId);
        Task SendFlightUpdateAsync(int flightId, string updateType, string message);
        Task SendCancellationNotificationAsync(int bookingId);
        Task SendPaymentConfirmationAsync(int paymentId);
        Task<List<NotificationDto>> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 20);
        Task<bool> MarkAsReadAsync(int notificationId, int userId);
        Task<int> GetUnreadCountAsync(int userId);
    }
}
