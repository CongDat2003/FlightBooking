namespace FlightBooking.Services
{
    public interface IPushNotificationService
    {
        Task SendAsync(int userId, string title, string message);
        Task SendToDeviceAsync(string deviceToken, string title, string message);
        Task SendToTopicAsync(string topic, string title, string message);
    }
}
