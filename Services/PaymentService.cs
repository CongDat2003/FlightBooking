using FlightBooking.Configuration;
using FlightBooking.DTOs;
//using FlightBooking.Helpers;
using FlightBooking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace FlightBooking.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly FlightBookingContext _context;
        //private readonly IOptions<VNPayConfig> _vnpayConfig;
        private readonly IOptions<MoMoConfig> _momoConfig;
        private readonly IOptions<ZaloPayConfig> _zalopayConfig;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            FlightBookingContext context,
            //IOptions<VNPayConfig> vnpayConfig,
            IOptions<MoMoConfig> momoConfig,
            IOptions<ZaloPayConfig> zalopayConfig,
            ILogger<PaymentService> logger)
        {
            _context = context;
           // _vnpayConfig = vnpayConfig;
            _momoConfig = momoConfig;
            _zalopayConfig = zalopayConfig;
            _logger = logger;
        }

        public async Task<PaymentResponseDto> CreatePaymentAsync(CreatePaymentDto paymentDto)
        {
            var booking = await _context.Bookings.FindAsync(paymentDto.BookingId);
            if (booking == null)
                throw new ArgumentException("Booking not found");

            var transactionId = GenerateTransactionId();

            var payment = new Payment
            {
                BookingId = paymentDto.BookingId,
                PaymentMethod = paymentDto.PaymentMethod,
                TransactionId = transactionId,
                Amount = booking.TotalAmount,
                Status = "PENDING"
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Generate payment URL based on payment method
            var paymentUrl = await GeneratePaymentUrlAsync(payment, paymentDto);

            return new PaymentResponseDto
            {
                PaymentId = payment.PaymentId,
                TransactionId = transactionId,
                PaymentUrl = paymentUrl,
                Status = payment.Status,
                Amount = payment.Amount,
                CreatedAt = payment.CreatedAt
            };
        }

        public async Task<PaymentResponseDto> ProcessCallbackAsync(PaymentCallbackDto callbackDto)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.TransactionId == callbackDto.TransactionId);

            if (payment == null)
                throw new ArgumentException("Payment not found");

            payment.Status = callbackDto.Status;
            payment.ProcessedAt = DateTime.Now;
            payment.PaymentData = System.Text.Json.JsonSerializer.Serialize(callbackDto.AdditionalData);

            if (callbackDto.Status == "SUCCESS")
            {
                payment.Booking.PaymentStatus = "PAID";
            }
            else if (callbackDto.Status == "FAILED")
            {
                payment.Booking.PaymentStatus = "FAILED";
            }

            await _context.SaveChangesAsync();

            return new PaymentResponseDto
            {
                PaymentId = payment.PaymentId,
                TransactionId = payment.TransactionId,
                Status = payment.Status,
                Amount = payment.Amount,
                CreatedAt = payment.CreatedAt
            };
        }

        private async Task<string> GeneratePaymentUrlAsync(Payment payment, CreatePaymentDto paymentDto)
        {
            switch (paymentDto.PaymentMethod.ToUpper())
            {
                //case "VNPAY":return await GenerateVNPayUrlAsync(payment, paymentDto);
                case "MOMO":
                    return await GenerateMoMoUrlAsync(payment, paymentDto);
                case "ZALOPAY":
                    return await GenerateZaloPayUrlAsync(payment, paymentDto);
                default:
                    throw new NotSupportedException($"Payment method {paymentDto.PaymentMethod} is not supported");
            }
        }
        /*
                private async Task<string> GenerateVNPayUrlAsync(Payment payment, CreatePaymentDto paymentDto)
                {
                    var config = _vnpayConfig.Value;

                    var vnpay = new VnPayLibrary();
                    vnpay.AddRequestData("vnp_Version", config.Version);
                    vnpay.AddRequestData("vnp_Command", config.Command);
                    vnpay.AddRequestData("vnp_TmnCode", config.TmnCode);
                    vnpay.AddRequestData("vnp_Amount", ((long)(payment.Amount * 100)).ToString());
                    vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
                    vnpay.AddRequestData("vnp_CurrCode", config.CurrCode);
                    vnpay.AddRequestData("vnp_IpAddr", "127.0.0.1");
                    vnpay.AddRequestData("vnp_Locale", config.Locale);
                    vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan ve may bay - Ma booking: {payment.Booking.BookingReference}");
                    vnpay.AddRequestData("vnp_OrderType", "other");
                    vnpay.AddRequestData("vnp_ReturnUrl", paymentDto.ReturnUrl ?? config.ReturnUrl);
                    vnpay.AddRequestData("vnp_TxnRef", payment.TransactionId);

                    return vnpay.CreateRequestUrl(config.Url, config.HashSecret);
                }*/

        // Services/PaymentService.cs - Sửa method GenerateVNPayUrlAsync
/*        private async Task<string> GenerateVNPayUrlAsync(Payment payment, CreatePaymentDto paymentDto)
        {
            var config = _vnpayConfig.Value;
            var booking = await _context.Bookings.FindAsync(payment.BookingId);

            var vnpay = new VnPayLibrary();
            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", config.TmnCode);
            vnpay.AddRequestData("vnp_Amount", ((long)(payment.Amount * 100)).ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", "127.0.0.1");
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan ve may bay - Ma booking: {booking?.BookingReference ?? payment.TransactionId}");
            vnpay.AddRequestData("vnp_OrderType", "other");

            // SỬA: Ưu tiên ReturnUrl từ request, fallback về config
            var returnUrl = !string.IsNullOrEmpty(paymentDto.ReturnUrl)
                ? paymentDto.ReturnUrl
                : config.ReturnUrl;
            vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);

            vnpay.AddRequestData("vnp_TxnRef", payment.TransactionId);

            return vnpay.CreateRequestUrl(config.Url, config.HashSecret);
        }*/


        /*private async Task<string> GenerateMoMoUrlAsync(Payment payment, CreatePaymentDto paymentDto)
        {
            var config = _momoConfig.Value;
            var orderId = payment.TransactionId;
            var amount = payment.Amount.ToString();
            var orderInfo = $"Thanh toán vé máy bay - {payment.TransactionId}";
            var redirectUrl = paymentDto.ReturnUrl ?? config.ReturnUrl;
            var ipnUrl = config.IpnUrl;
            var requestType = config.RequestType;
            var extraData = "";

            // Create raw signature
            var rawSignature = $"accessKey={config.AccessKey}&amount={amount}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={config.PartnerCode}&redirectUrl={redirectUrl}&requestId={orderId}&requestType={requestType}";

            var signature = ComputeHmacSha256(rawSignature, config.SecretKey);

            var requestData = new
            {
                partnerCode = config.PartnerCode,
                partnerName = "Flight Booking",
                storeId = "FlightBookingStore",
                requestId = orderId,
                amount = long.Parse(amount),
                orderId = orderId,
                orderInfo = orderInfo,
                redirectUrl = redirectUrl,
                ipnUrl = ipnUrl,
                lang = "vi",
                extraData = extraData,
                requestType = requestType,
                signature = signature
            };

            using var httpClient = new HttpClient();
            var json = System.Text.Json.JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(config.Endpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            var momoResponse = System.Text.Json.JsonSerializer.Deserialize<MoMoResponse>(responseContent);

            return momoResponse?.payUrl ?? "";
        }*/

        private async Task<string> GenerateMoMoUrlAsync(Payment payment, CreatePaymentDto paymentDto)
        {
            var config = _momoConfig.Value;
            var orderId = payment.TransactionId;
            var requestId = payment.TransactionId;
            var amount = ((long)payment.Amount).ToString(CultureInfo.InvariantCulture);
            var orderInfo = $"Thanh toán vé máy bay - {payment.TransactionId}";
            var redirectUrl = paymentDto.ReturnUrl ?? config.ReturnUrl;
            var ipnUrl = config.IpnUrl;
            var requestType = config.RequestType;
            var extraData = "";

            // Tạo raw signature theo thứ tự alphabet
            var rawSignature = $"accessKey={config.AccessKey}&amount={amount}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={config.PartnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}";

            var signature = ComputeHmacSha256(rawSignature, config.SecretKey);

            var requestData = new
            {
                partnerCode = config.PartnerCode,
                partnerName = "Flight Booking",
                storeId = "FlightBookingStore",
                requestId = requestId,
                amount = (long)payment.Amount,
                orderId = orderId,
                orderInfo = orderInfo,
                redirectUrl = redirectUrl,
                ipnUrl = ipnUrl,
                lang = "vi",
                extraData = extraData,
                requestType = requestType,
                signature = signature
            };

            using var httpClient = new HttpClient();
            var json = System.Text.Json.JsonSerializer.Serialize(requestData, new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation($"MoMo request: {json}");

            var response = await httpClient.PostAsync(config.Endpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"MoMo response: {responseContent}");

            var momoResponse = System.Text.Json.JsonSerializer.Deserialize<MoMoResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // SỬA: So sánh với int thay vì string
            if (momoResponse?.resultCode == 0)
            {
                return momoResponse.payUrl;
            }

            throw new Exception($"MoMo error: {momoResponse?.message} (Code: {momoResponse?.resultCode})");
        }


        /*       private async Task<string> GenerateZaloPayUrlAsync(Payment payment, CreatePaymentDto paymentDto)
               {
                   var config = _zalopayConfig.Value;
                   var embedData = "{}";
                   var items = "[]";
                   var transID = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                   var order = new Dictionary<string, object>
                   {
                       {"app_id", int.Parse(config.AppId)},
                       {"app_trans_id", $"{DateTime.Now:yyMMdd}_{payment.TransactionId}"},
                       {"app_user", "user123"},
                       {"app_time", transID},
                       {"embed_data", embedData},
                       {"item", items},
                       {"amount", (long)payment.Amount},
                       {"description", $"Thanh toán vé máy bay - {payment.TransactionId}"},
                       {"bank_code", ""},
                       {"callback_url", config.CallbackUrl}
                   };

                   var data = $"{order["app_id"]}|{order["app_trans_id"]}|{order["app_user"]}|{order["amount"]}|{order["app_time"]}|{order["embed_data"]}|{order["item"]}";
                   order["mac"] = ComputeHmacSha256(data, config.Key1);

                   using var httpClient = new HttpClient();
                   var json = System.Text.Json.JsonSerializer.Serialize(order);
                   var content = new StringContent(json, Encoding.UTF8, "application/json");

                   var response = await httpClient.PostAsync(config.Endpoint, content);
                   var responseContent = await response.Content.ReadAsStringAsync();

                   var zaloResponse = System.Text.Json.JsonSerializer.Deserialize<ZaloPayResponse>(responseContent);

                   return zaloResponse?.order_url ?? "";
               }*/

        private async Task<string> GenerateZaloPayUrlAsync(Payment payment, CreatePaymentDto paymentDto)
        {
            var config = _zalopayConfig.Value;
            var embedData = "{}";
            var items = "[]";
            var transID = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var appTransId = $"{DateTime.Now:yyMMdd}_{payment.TransactionId}";

            var order = new Dictionary<string, object>
            {
                {"app_id", int.Parse(config.AppId)},
                {"app_trans_id", appTransId},
                {"app_user", "user123"},
                {"app_time", transID},
                {"embed_data", embedData},
                {"item", items},
                {"amount", (long)payment.Amount},
                {"description", $"Thanh toán vé máy bay - {payment.TransactionId}"},
                {"bank_code", ""},
                {"callback_url", config.CallbackUrl}
            };

            // Tạo MAC
            var data = $"{order["app_id"]}|{order["app_trans_id"]}|{order["app_user"]}|{order["amount"]}|{order["app_time"]}|{order["embed_data"]}|{order["item"]}";
            order["mac"] = ComputeHmacSha256(data, config.Key1);

            using var httpClient = new HttpClient();
            var json = System.Text.Json.JsonSerializer.Serialize(order);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(config.Endpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"ZaloPay response: {responseContent}");

            var zaloResponse = System.Text.Json.JsonSerializer.Deserialize<ZaloPayResponse>(responseContent);

            if (zaloResponse?.return_code == 1)
            {
                return zaloResponse.order_url;
            }

            throw new Exception($"ZaloPay error: {zaloResponse?.return_message}");
        }

        private string ComputeHmacSha256(string message, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(messageBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        private string GenerateTransactionId()
        {
            return $"TXN{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        }

        public async Task<PaymentResponseDto> GetPaymentStatusAsync(string transactionId)
        {
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.TransactionId == transactionId);
            if (payment == null)
                throw new ArgumentException("Payment not found");

            return new PaymentResponseDto
            {
                PaymentId = payment.PaymentId,
                TransactionId = payment.TransactionId,
                Status = payment.Status,
                Amount = payment.Amount,
                CreatedAt = payment.CreatedAt
            };
        }

        public async Task<bool> RefundPaymentAsync(int paymentId, decimal? refundAmount = null)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null || payment.Status != "SUCCESS")
                return false;

            var refund = refundAmount ?? payment.Amount;

            // Process refund with payment gateway
            // Implementation depends on payment method

            payment.Status = "REFUNDED";
            payment.Booking.PaymentStatus = "REFUNDED";

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
