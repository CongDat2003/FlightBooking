namespace FlightBooking.Services
{
    public interface ISMSService
    {
        Task SendAsync(string phoneNumber, string message);
        Task SendBookingConfirmationSMSAsync(string phoneNumber, string bookingReference);
        Task SendFlightUpdateSMSAsync(string phoneNumber, string flightNumber, string message);
    }
}
