using System.ComponentModel.DataAnnotations;

namespace FlightBooking.DTOs
{
    public class CreatePaymentDto
    {
        [Required]
        public int BookingId { get; set; }
        
        [Required]
        public string PaymentMethod { get; set; }
        
        // ReturnUrl và CancelUrl là optional - backend sẽ tự động detect hoặc dùng config nếu null/empty
        // Nếu không set, backend sẽ dùng ReturnUrl từ config hoặc detect từ HttpContext
        public string? ReturnUrl { get; set; }
        public string? CancelUrl { get; set; }
        
        // Optional: VNPay channel selector (sandbox demo): VNPAYQR | VNBANK | INTCARD
        public string? BankCode { get; set; }
    }

    public class PaymentResponseDto
    {
        public int BookingId { get; set; }
        public int PaymentId { get; set; }
        public string TransactionId { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentUrl { get; set; }
        public string Status { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdatePaymentDto
    {
        public string? PaymentMethod { get; set; }
        public decimal? Amount { get; set; }
        public string? Status { get; set; } // PENDING, SUCCESS, FAILED, REFUNDED
        public string? Notes { get; set; }
    }

    public class PaymentCallbackDto
    {
        public string TransactionId { get; set; }
        public string Status { get; set; }
        public string ResponseCode { get; set; }
        public string Message { get; set; }
        public Dictionary<string, string> AdditionalData { get; set; }
    }

    public class MoMoResponse
    {
        public string partnerCode { get; set; }
        public string orderId { get; set; }
        public string requestId { get; set; }
        public long amount { get; set; }
        public long responseTime { get; set; }
        public string message { get; set; }
        public int resultCode { get; set; }
        public string payUrl { get; set; }
        public string deeplink { get; set; }
        public string qrCodeUrl { get; set; }
    }

    public class ZaloPayResponse
    {
        public int return_code { get; set; }
        public string return_message { get; set; }
        public int sub_return_code { get; set; }
        public string sub_return_message { get; set; }
        public string order_url { get; set; }
        public string zp_trans_token { get; set; }
        public string order_token { get; set; }
        public string qr_code { get; set; }
    }
}
