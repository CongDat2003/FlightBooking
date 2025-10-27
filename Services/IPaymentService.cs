using FlightBooking.DTOs;
using FlightBooking.Models;

namespace FlightBooking.Services
{
    public interface IPaymentService
    {
        Task<List<PaymentResponseDto>> GetAllPaymentsAsync();
        Task<PaymentResponseDto> GetPaymentByIdAsync(int paymentId);
        Task<PaymentResponseDto> CreatePaymentAsync(CreatePaymentDto paymentDto);
        Task<PaymentResponseDto> ProcessCallbackAsync(PaymentCallbackDto callbackDto);
        Task<PaymentResponseDto> GetPaymentStatusAsync(string transactionId);
        Task<bool> RefundPaymentAsync(int paymentId, decimal? refundAmount = null);
    }
}
