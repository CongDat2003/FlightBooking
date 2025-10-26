namespace FlightBooking.Services
{
    public class SMSService : ISMSService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SMSService> _logger;

        public SMSService(IConfiguration configuration, ILogger<SMSService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendAsync(string phoneNumber, string message)
        {
            try
            {
                // Implement actual SMS sending logic here
                // For now, just log
                _logger.LogInformation($"SMS sent to {phoneNumber} - Message: {message}");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send SMS to {phoneNumber}");
            }
        }

        public async Task SendBookingConfirmationSMSAsync(string phoneNumber, string bookingReference)
        {
            var message = $"Dat ve thanh cong! Ma dat cho: {bookingReference}. Cam on ban da su dung dich vu cua chung toi.";
            await SendAsync(phoneNumber, message);
        }

        public async Task SendFlightUpdateSMSAsync(string phoneNumber, string flightNumber, string message)
        {
            var smsMessage = $"Cap nhat chuyen bay {flightNumber}: {message}";
            await SendAsync(phoneNumber, smsMessage);
        }
    }
}
