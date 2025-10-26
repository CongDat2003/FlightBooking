namespace FlightBooking.Services
{
    public interface IEmailService
    {
        Task SendBookingConfirmationEmailAsync(Models.Booking booking);
        Task SendFlightUpdateEmailAsync(string email, string subject, string message);
        Task SendCancellationEmailAsync(Models.Booking booking);
        Task SendPaymentConfirmationEmailAsync(Models.Payment payment);
        Task SendPasswordResetOtpEmailAsync(string recipientEmail, string otpCode, string recipientName);
    }
}
