using FlightBooking.DTOs;

namespace FlightBooking.Services
{
    public interface IFlightService
    {
        Task<List<FlightResponseDto>> SearchFlightsAsync(FlightSearchDto searchDto);
        Task<SeatMapDto> GetSeatMapAsync(int flightId, int userId);
        Task<BookingResponseDto> CreateBookingAsync(CreateBookingDto bookingDto);
        Task<List<BookingResponseDto>> GetAllBookingsAsync();
        Task<List<BookingResponseDto>> GetUserBookingsAsync(int userId);
        Task<BookingResponseDto> GetBookingByIdAsync(int bookingId);
        Task<bool> UpdateBookingStatusAsync(int bookingId, string status);
        Task<bool> CancelBookingAsync(int bookingId);
        Task<bool> ConfirmPaymentAsync(int paymentId);
    }
}
