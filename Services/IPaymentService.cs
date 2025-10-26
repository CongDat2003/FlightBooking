using FlightBooking.DTOs;

namespace FlightBooking.Services
{
    public interface IPaymentService
    {
        Task<PaymentResponseDto> CreatePaymentAsync(CreatePaymentDto paymentDto);
        Task<PaymentResponseDto> ProcessCallbackAsync(PaymentCallbackDto callbackDto);
        Task<PaymentResponseDto> GetPaymentStatusAsync(string transactionId);
        Task<bool> RefundPaymentAsync(int paymentId, decimal? refundAmount = null);
    }
}
