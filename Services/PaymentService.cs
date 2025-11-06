using FlightBooking.Configuration;
using FlightBooking.DTOs;
using FlightBooking.Helpers;
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
        private readonly IOptions<VNPayConfig> _vnpayConfig;
        private readonly IOptions<ZaloPayConfig> _zalopayConfig;
        private readonly ILogger<PaymentService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PaymentService(
            FlightBookingContext context,
            IOptions<VNPayConfig> vnpayConfig,
            IOptions<ZaloPayConfig> zalopayConfig,
            ILogger<PaymentService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _vnpayConfig = vnpayConfig;
            _zalopayConfig = zalopayConfig;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<PaymentResponseDto> CreatePaymentAsync(CreatePaymentDto paymentDto)
        {
            _logger.LogInformation($"=== CreatePaymentAsync START ===");
            _logger.LogInformation($"BookingId: {paymentDto.BookingId}, PaymentMethod: {paymentDto.PaymentMethod}");
            
            var booking = await _context.Bookings.FindAsync(paymentDto.BookingId);
            if (booking == null)
            {
                _logger.LogError($"Booking not found: {paymentDto.BookingId}");
                throw new ArgumentException("Booking not found");
            }

            var transactionId = GenerateTransactionId();
            _logger.LogInformation($"Generated TransactionId: {transactionId}");

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
            _logger.LogInformation($"Payment created in DB: PaymentId={payment.PaymentId}");

            // Generate payment URL based on payment method
            string paymentUrl;
            try
            {
                _logger.LogInformation($"Starting URL generation for method: {paymentDto.PaymentMethod}");
                paymentUrl = await GeneratePaymentUrlAsync(payment, paymentDto);
                
                if (string.IsNullOrWhiteSpace(paymentUrl))
                {
                    _logger.LogError($"Payment URL generation returned empty/null for PaymentId={payment.PaymentId}");
                    // Rollback payment
                    _context.Payments.Remove(payment);
                    await _context.SaveChangesAsync();
                    throw new InvalidOperationException("Không thể tạo URL thanh toán. Vui lòng thử lại sau.");
                }
                
                _logger.LogInformation($"Payment URL generated successfully: {paymentUrl}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating payment URL for PaymentId={payment.PaymentId}, Method={paymentDto.PaymentMethod}");
                // Rollback payment if URL generation fails
                try
                {
                    _context.Payments.Remove(payment);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Payment rolled back: PaymentId={payment.PaymentId}");
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, $"Error rolling back payment: PaymentId={payment.PaymentId}");
                }
                throw;
            }
            
            // Save PaymentUrl to database
            payment.PaymentUrl = paymentUrl;
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Payment URL saved to DB: PaymentId={payment.PaymentId}");

            _logger.LogInformation($"=== CreatePaymentAsync SUCCESS - PaymentId={payment.PaymentId} ===");
            return new PaymentResponseDto
            {
                BookingId = payment.BookingId,
                PaymentId = payment.PaymentId,
                TransactionId = transactionId,
                PaymentMethod = payment.PaymentMethod,
                PaymentUrl = paymentUrl,
                Status = payment.Status,
                Amount = payment.Amount,
                CreatedAt = payment.CreatedAt,
                Notes = payment.Notes
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
                if (payment.Booking.BookingStatus != "CONFIRMED")
                {
                    payment.Booking.BookingStatus = "CONFIRMED";
                }
            }
            else if (callbackDto.Status == "FAILED")
            {
                payment.Booking.PaymentStatus = "FAILED";
            }

            await _context.SaveChangesAsync();

            return new PaymentResponseDto
            {
                BookingId = payment.BookingId,
                PaymentId = payment.PaymentId,
                TransactionId = payment.TransactionId,
                PaymentMethod = payment.PaymentMethod,
                Status = payment.Status,
                Amount = payment.Amount,
                CreatedAt = payment.CreatedAt,
                Notes = payment.Notes
            };
        }

        private async Task<string> GeneratePaymentUrlAsync(Payment payment, CreatePaymentDto paymentDto)
        {
            switch (paymentDto.PaymentMethod.ToUpper())
            {
                case "VNPAY":
                    return await GenerateVNPayUrlAsync(payment, paymentDto);
                case "ZALOPAY":
                    return await GenerateZaloPayUrlAsync(payment, paymentDto);
                default:
                    throw new NotSupportedException($"Payment method {paymentDto.PaymentMethod} is not supported");
            }
        }
        private async Task<string> GenerateVNPayUrlAsync(Payment payment, CreatePaymentDto paymentDto)
        {
            _logger.LogInformation($"=== GenerateVNPayUrlAsync START ===");
            _logger.LogInformation($"PaymentId: {payment.PaymentId}, TransactionId: {payment.TransactionId}, Amount: {payment.Amount}");
            
            try
            {
                var config = _vnpayConfig.Value;
                
                // Validate config
                if (config == null)
                {
                    _logger.LogError("VNPay config is null");
                    throw new InvalidOperationException("Cấu hình VNPay không hợp lệ");
                }
                
                if (string.IsNullOrWhiteSpace(config.TmnCode))
                {
                    _logger.LogError("VNPay TmnCode is empty");
                    throw new InvalidOperationException("VNPay TmnCode chưa được cấu hình");
                }
                
                if (string.IsNullOrWhiteSpace(config.HashSecret))
                {
                    _logger.LogError("VNPay HashSecret is empty");
                    throw new InvalidOperationException("VNPay HashSecret chưa được cấu hình");
                }
                
                if (string.IsNullOrWhiteSpace(config.Url))
                {
                    _logger.LogError("VNPay Url is empty");
                    throw new InvalidOperationException("VNPay Url chưa được cấu hình");
                }
                
                // Validate and fix ReturnUrl
                var returnUrl = config.ReturnUrl;
                _logger.LogInformation($"Original ReturnUrl from config: {returnUrl}");
                
                if (string.IsNullOrWhiteSpace(returnUrl))
                {
                    _logger.LogWarning("ReturnUrl is empty in config, using default");
                    returnUrl = "http://172.20.10.5:501/api/payment/vnpay-return";
                }
                else if (returnUrl.Contains("localhost") || returnUrl.Contains("127.0.0.1") || returnUrl.Contains("192.168.1.7") || returnUrl.Contains("192.168.10.73") || returnUrl.Contains("192.168.10.9"))
                {
                    _logger.LogWarning($"ReturnUrl contains old/localhost IP: {returnUrl}, replacing with new IP");
                    returnUrl = returnUrl.Replace("localhost", "172.20.10.5")
                                          .Replace("127.0.0.1", "172.20.10.5")
                                          .Replace("192.168.1.7", "172.20.10.5")
                                          .Replace("192.168.10.73", "172.20.10.5")
                                          .Replace("192.168.10.9", "172.20.10.5");
                }
                
                if (!returnUrl.StartsWith("http://") && !returnUrl.StartsWith("https://"))
                {
                    _logger.LogError($"ReturnUrl is not a valid HTTP(S) URL: {returnUrl}");
                    throw new InvalidOperationException("ReturnUrl phải là một HTTP/HTTPS URL hợp lệ");
                }
                
                _logger.LogInformation($"Final ReturnUrl: {returnUrl}");
                
                var booking = await _context.Bookings.FindAsync(payment.BookingId);
                if (booking == null)
                {
                    _logger.LogWarning($"Booking not found for PaymentId={payment.PaymentId}, BookingId={payment.BookingId}");
                }

                var vnpay = new VnPayLibrary();
                
                // Thông tin cơ bản
                vnpay.AddRequestData("vnp_Version", "2.1.0");
                vnpay.AddRequestData("vnp_Command", "pay");
                vnpay.AddRequestData("vnp_TmnCode", config.TmnCode);
                
                // Số tiền (nhân 100 vì VNPay yêu cầu đơn vị là xu)
                var amountInVnd = ((long)(payment.Amount * 100)).ToString();
                vnpay.AddRequestData("vnp_Amount", amountInVnd);
                _logger.LogInformation($"Amount in VND (x100): {amountInVnd}");
                
                // Thời gian tạo giao dịch
                var createDate = DateTime.Now;
                var createDateStr = createDate.ToString("yyyyMMddHHmmss");
                vnpay.AddRequestData("vnp_CreateDate", createDateStr);
                _logger.LogInformation($"CreateDate: {createDateStr}");
                
                // Thời gian hết hạn (15 phút sau)
                var expireDate = createDate.AddMinutes(15);
                var expireDateStr = expireDate.ToString("yyyyMMddHHmmss");
                vnpay.AddRequestData("vnp_ExpireDate", expireDateStr);
                _logger.LogInformation($"ExpireDate: {expireDateStr}");
                
                // Thông tin tiền tệ và địa chỉ IP
                vnpay.AddRequestData("vnp_CurrCode", "VND");
                var clientIp = GetClientIpAddress();
                vnpay.AddRequestData("vnp_IpAddr", clientIp);
                _logger.LogInformation($"Client IP: {clientIp}");
                vnpay.AddRequestData("vnp_Locale", "vn");
                
                // Thông tin đơn hàng
                var orderInfo = $"Thanh toan ve may bay - Ma booking: {booking?.BookingReference ?? payment.TransactionId}";
                vnpay.AddRequestData("vnp_OrderInfo", orderInfo);
                _logger.LogInformation($"OrderInfo: {orderInfo}");
                vnpay.AddRequestData("vnp_OrderType", "other");
                
                // Tùy chọn kênh thanh toán sandbox: VNPAYQR | VNBANK | INTCARD
                if (!string.IsNullOrWhiteSpace(paymentDto.BankCode))
                {
                    var bankCode = paymentDto.BankCode.Trim().ToUpperInvariant();
                    vnpay.AddRequestData("vnp_BankCode", bankCode);
                    _logger.LogInformation($"BankCode: {bankCode}");
                }
                else
                {
                    _logger.LogInformation("No BankCode - VNPay will show all payment methods");
                }
                
                // URL trả về: luôn dùng cấu hình HTTP/HTTPS để VNPay redirect (WebView sẽ chặn deep link)
                vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
                
                // Mã giao dịch
                vnpay.AddRequestData("vnp_TxnRef", payment.TransactionId);
                _logger.LogInformation($"TxnRef: {payment.TransactionId}");

                _logger.LogInformation($"VNPay Config - TmnCode: {config.TmnCode}, Url: {config.Url}");
                _logger.LogInformation($"VNPay Config - ReturnUrl: {returnUrl}");
                _logger.LogInformation($"VNPay Config - HashSecret length: {config.HashSecret?.Length ?? 0}");

                // Generate payment URL
                string paymentUrl;
                try
                {
                    paymentUrl = vnpay.CreateRequestUrl(config.Url, config.HashSecret);
                    
                    if (string.IsNullOrWhiteSpace(paymentUrl))
                    {
                        _logger.LogError("VNPay CreateRequestUrl returned null or empty");
                        throw new InvalidOperationException("Không thể tạo URL thanh toán VNPay. Vui lòng kiểm tra cấu hình.");
                    }
                    
                    _logger.LogInformation($"VNPay URL generated successfully");
                    _logger.LogInformation($"Generated URL length: {paymentUrl.Length}");
                    _logger.LogInformation($"Generated URL (first 100 chars): {paymentUrl.Substring(0, Math.Min(100, paymentUrl.Length))}...");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error calling VnPayLibrary.CreateRequestUrl - Url: {config.Url}");
                    throw new InvalidOperationException($"Lỗi tạo URL thanh toán VNPay: {ex.Message}", ex);
                }
                
                _logger.LogInformation($"=== GenerateVNPayUrlAsync SUCCESS ===");
                return paymentUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"=== GenerateVNPayUrlAsync FAILED - PaymentId={payment.PaymentId} ===");
                throw;
            }
        }

        private string GetClientIpAddress()
        {
            try
            {
                // Lấy IP từ HttpContext thông qua IHttpContextAccessor
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.Request != null)
                {
                    // Kiểm tra X-Forwarded-For header (khi có proxy/load balancer)
                    if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var xForwardedFor))
                    {
                        var ip = xForwardedFor.ToString().Split(',')[0].Trim();
                        if (!string.IsNullOrEmpty(ip) && ip != "unknown")
                        {
                            return ip;
                        }
                    }
                    
                    // Kiểm tra X-Real-IP header
                    if (httpContext.Request.Headers.TryGetValue("X-Real-IP", out var xRealIp))
                    {
                        var ip = xRealIp.ToString();
                        if (!string.IsNullOrEmpty(ip) && ip != "unknown")
                        {
                            return ip;
                        }
                    }
                    
                    // Lấy IP trực tiếp từ request
                    var remoteIpAddress = httpContext.Connection.RemoteIpAddress;
                    if (remoteIpAddress != null)
                    {
                        var ip = remoteIpAddress.ToString();
                        if (ip != "::1" && ip != "127.0.0.1")
                        {
                            return ip;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error getting client IP: {ex.Message}");
            }
            
            // Fallback về IP mặc định (có thể thay bằng IP thật của server)
            return "127.0.0.1";
        }



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
                BookingId = payment.BookingId,
                PaymentId = payment.PaymentId,
                TransactionId = payment.TransactionId,
                PaymentMethod = payment.PaymentMethod,
                Status = payment.Status,
                Amount = payment.Amount,
                CreatedAt = payment.CreatedAt,
                Notes = payment.Notes
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

        public async Task<List<PaymentResponseDto>> GetAllPaymentsAsync()
        {
            var payments = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.User)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Flight)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return payments.Select(p => new PaymentResponseDto
            {
                BookingId = p.BookingId,
                PaymentId = p.PaymentId,
                TransactionId = p.TransactionId,
                PaymentMethod = p.PaymentMethod,
                PaymentUrl = p.PaymentUrl,
                Status = p.Status,
                Amount = p.Amount,
                CreatedAt = p.CreatedAt,
                Notes = p.Notes
            }).ToList();
        }

        public async Task<PaymentResponseDto> GetPaymentByIdAsync(int paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.User)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Flight)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null)
                throw new ArgumentException("Payment not found");

            return new PaymentResponseDto
            {
                BookingId = payment.BookingId,
                PaymentId = payment.PaymentId,
                TransactionId = payment.TransactionId,
                PaymentUrl = payment.PaymentUrl,
                PaymentMethod = payment.PaymentMethod,
                Status = payment.Status,
                Amount = payment.Amount,
                CreatedAt = payment.CreatedAt,
                Notes = payment.Notes
            };
        }

        public async Task<bool> DeletePaymentAsync(int paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null) return false;

            if (payment.Status == "SUCCESS")
                throw new InvalidOperationException("Cannot delete successful payment. Please refund instead.");

            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<PaymentResponseDto> UpdatePaymentAsync(int paymentId, UpdatePaymentDto dto)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null)
                throw new ArgumentException("Payment not found");

            // Business rules
            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                payment.Status = dto.Status.Trim().ToUpperInvariant();
            }
            if (dto.Amount.HasValue)
            {
                // Only block if trying to change the value on SUCCESS payments
                var isChangingAmount = dto.Amount.Value != payment.Amount;
                if (payment.Status == "SUCCESS" && isChangingAmount)
                    throw new InvalidOperationException("Cannot change amount of a successful payment");
                if (isChangingAmount)
                    payment.Amount = dto.Amount.Value;
            }
            if (!string.IsNullOrWhiteSpace(dto.PaymentMethod))
            {
                var newMethod = dto.PaymentMethod.Trim().ToUpperInvariant();
                var isChangingMethod = !string.Equals(payment.PaymentMethod, newMethod, StringComparison.OrdinalIgnoreCase);
                // Cho phép đổi phương thức ngay cả khi SUCCESS (không đổi amount)
                if (isChangingMethod)
                    payment.PaymentMethod = newMethod;
            }
            if (!string.IsNullOrWhiteSpace(dto.Notes))
            {
                payment.Notes = dto.Notes;
            }

            // Sync booking if status becomes SUCCESS/REFUNDED/FAILED
            if (payment.Booking != null)
            {
                if (payment.Status == "SUCCESS")
                {
                    payment.Booking.PaymentStatus = "PAID";
                    if (payment.Booking.BookingStatus != "CONFIRMED")
                        payment.Booking.BookingStatus = "CONFIRMED";
                }
                else if (payment.Status == "REFUNDED")
                {
                    payment.Booking.PaymentStatus = "REFUNDED";
                }
                else if (payment.Status == "FAILED")
                {
                    payment.Booking.PaymentStatus = "FAILED";
                }
                else if (payment.Status == "PENDING")
                {
                    payment.Booking.PaymentStatus = "PENDING";
                }
            }

            await _context.SaveChangesAsync();

            return new PaymentResponseDto
            {
                BookingId = payment.BookingId,
                PaymentId = payment.PaymentId,
                TransactionId = payment.TransactionId,
                PaymentUrl = payment.PaymentUrl,
                PaymentMethod = payment.PaymentMethod,
                Status = payment.Status,
                Amount = payment.Amount,
                CreatedAt = payment.CreatedAt,
                Notes = payment.Notes
            };
        }
    }
}
