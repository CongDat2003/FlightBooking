/*using FlightBooking.Configuration;
using FlightBooking.DTOs;
using FlightBooking.Helpers;
using FlightBooking.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text;

namespace FlightBooking.Controllers
{
    [ApiController]
    [Route("api/payment")]
    public class PaymentCallbackController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IOptions<VNPayConfig> _vnpayConfig;
        private readonly IOptions<ZaloPayConfig> _zalopayConfig; 
        private readonly IOptions<MoMoConfig> _momoConfig; 
        private readonly ILogger<PaymentCallbackController> _logger; 

        public PaymentCallbackController(
            IPaymentService paymentService,
            IOptions<VNPayConfig> vnpayConfig,
            IOptions<ZaloPayConfig> zalopayConfig, 
            IOptions<MoMoConfig> momoConfig, 
            ILogger<PaymentCallbackController> logger) 
        {
            _paymentService = paymentService;
            _vnpayConfig = vnpayConfig;
            _zalopayConfig = zalopayConfig; 
            _momoConfig = momoConfig; 
            _logger = logger; 
        }

        // Controllers/PaymentCallbackController.cs - Sửa VNPayReturn
        // Controllers/PaymentCallbackController.cs
        [HttpGet("vnpay-return")]
        [HttpPost("vnpay-return")]
        public async Task<IActionResult> VNPayReturn()
        {
            try
            {
                var vnpay = new VnPayLibrary();

                // Kiểm tra có query parameters không
                if (!Request.Query.Any())
                {
                    _logger.LogWarning("VNPay callback: No query parameters received");
                    return BadRequest(new
                    {
                        success = false,
                        message = "Không nhận được thông tin từ VNPay",
                        debug = "No query parameters"
                    });
                }

                // Log tất cả parameters để debug
                _logger.LogInformation($"VNPay callback parameters count: {Request.Query.Count}");
                foreach (var param in Request.Query)
                {
                    _logger.LogInformation($"{param.Key}: {param.Value}");

                    // Thêm vào VnPayLibrary
                    if (!string.IsNullOrEmpty(param.Key) && param.Key.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(param.Key, param.Value);
                    }
                }

                var vnp_HashSecret = _vnpayConfig.Value.HashSecret;
                var vnpSecureHash = Request.Query["vnp_SecureHash"].ToString();
                var vnpTxnRef = Request.Query["vnp_TxnRef"].ToString();
                var vnpResponseCode = Request.Query["vnp_ResponseCode"].ToString();

                _logger.LogInformation($"Hash Secret: {vnp_HashSecret}");
                _logger.LogInformation($"Received Hash: {vnpSecureHash}");
                _logger.LogInformation($"Transaction Ref: {vnpTxnRef}");
                _logger.LogInformation($"Response Code: {vnpResponseCode}");

                // Kiểm tra các tham số bắt buộc
                if (string.IsNullOrEmpty(vnpSecureHash) || string.IsNullOrEmpty(vnpTxnRef))
                {
                    _logger.LogError("VNPay callback: Missing required parameters");
                    return BadRequest(new
                    {
                        success = false,
                        message = "Thiếu thông tin bắt buộc từ VNPay",
                        debug = $"Hash: {vnpSecureHash}, TxnRef: {vnpTxnRef}"
                    });
                }

                // Validate signature
                bool checkSignature = vnpay.ValidateSignature(vnpSecureHash, vnp_HashSecret);
                _logger.LogInformation($"Signature validation result: {checkSignature}");

                if (checkSignature)
                {
                    if (vnpResponseCode == "00")
                    {
                        var callbackDto = new PaymentCallbackDto
                        {
                            TransactionId = vnpTxnRef,
                            Status = "SUCCESS",
                            ResponseCode = vnpResponseCode,
                            Message = "Thanh toán thành công",
                            AdditionalData = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString())
                        };

                        await _paymentService.ProcessCallbackAsync(callbackDto);

                        return Ok(new
                        {
                            success = true,
                            message = "Thanh toán thành công",
                            transactionId = vnpTxnRef,
                            responseCode = vnpResponseCode
                        });
                    }
                    else
                    {
                        var callbackDto = new PaymentCallbackDto
                        {
                            TransactionId = vnpTxnRef,
                            Status = "FAILED",
                            ResponseCode = vnpResponseCode,
                            Message = GetVNPayResponseMessage(vnpResponseCode),
                            AdditionalData = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString())
                        };

                        await _paymentService.ProcessCallbackAsync(callbackDto);

                        return Ok(new
                        {
                            success = false,
                            message = GetVNPayResponseMessage(vnpResponseCode),
                            transactionId = vnpTxnRef,
                            responseCode = vnpResponseCode
                        });
                    }
                }
                else
                {
                    _logger.LogError("VNPay signature validation failed");
                    return BadRequest(new
                    {
                        success = false,
                        message = "Chữ ký không hợp lệ",
                        receivedHash = vnpSecureHash,
                        transactionId = vnpTxnRef,
                        debug = "Signature validation failed"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VNPay callback error");
                return BadRequest(new
                {
                    success = false,
                    message = "Lỗi xử lý callback",
                    error = ex.Message
                });
            }
        }

        private string GetVNPayResponseMessage(string responseCode)
        {
            return responseCode switch
            {
                "00" => "Giao dịch thành công",
                "07" => "Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường).",
                "09" => "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng chưa đăng ký dịch vụ InternetBanking tại ngân hàng.",
                "10" => "Giao dịch không thành công do: Khách hàng xác thực thông tin thẻ/tài khoản không đúng quá 3 lần",
                "11" => "Giao dịch không thành công do: Đã hết hạn chờ thanh toán. Xin quý khách vui lòng thực hiện lại giao dịch.",
                "12" => "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng bị khóa.",
                "13" => "Giao dịch không thành công do Quý khách nhập sai mật khẩu xác thực giao dịch (OTP).",
                "24" => "Giao dịch không thành công do: Khách hàng hủy giao dịch",
                "51" => "Giao dịch không thành công do: Tài khoản của quý khách không đủ số dư để thực hiện giao dịch.",
                "65" => "Giao dịch không thành công do: Tài khoản của Quý khách đã vượt quá hạn mức giao dịch trong ngày.",
                "75" => "Ngân hàng thanh toán đang bảo trì.",
                "79" => "Giao dịch không thành công do: KH nhập sai mật khẩu thanh toán quá số lần quy định.",
                "99" => "Các lỗi khác (lỗi còn lại, không có trong danh sách mã lỗi đã liệt kê)",
                _ => $"Lỗi không xác định: {responseCode}"
            };
        }

        [HttpPost("zalopay-callback")]
        public async Task<IActionResult> ZaloPayCallback([FromBody] ZaloPayCallbackDto callback)
        {
            try
            {
                var config = _zalopayConfig.Value;

                // Verify MAC
                var data = callback.data;
                var reqMac = callback.mac;
                var mac = ComputeHmacSha256(data, config.Key2);

                if (reqMac.Equals(mac))
                {
                    // Parse data
                    var dataObj = System.Text.Json.JsonSerializer.Deserialize<ZaloPayDataDto>(data);

                    var callbackDto = new PaymentCallbackDto
                    {
                        TransactionId = dataObj.app_trans_id.Split('_')[1], // Extract original transaction ID
                        Status = "SUCCESS",
                        ResponseCode = "00",
                        Message = "Thanh toán thành công",
                        AdditionalData = new Dictionary<string, string>
                        {
                            {"zp_trans_id", dataObj.zp_trans_id.ToString()},
                            {"app_trans_id", dataObj.app_trans_id}
                        }
                    };

                    await _paymentService.ProcessCallbackAsync(callbackDto);

                    return Ok(new { return_code = 1, return_message = "success" });
                }

                return Ok(new { return_code = -1, return_message = "mac not equal" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZaloPay callback error"); // SỬA: Thêm _logger
                return Ok(new { return_code = 0, return_message = "error" });
            }
        }

        // Controllers/PaymentCallbackController.cs - Sửa MoMoCallback method
        [HttpPost("momo-callback")]
        public async Task<IActionResult> MoMoCallback([FromBody] MoMoCallbackDto callback)
        {
            try
            {
                var config = _momoConfig.Value;

                // Verify signature
                var rawSignature = $"accessKey={config.AccessKey}&amount={callback.amount}&extraData={callback.extraData}&message={callback.message}&orderId={callback.orderId}&orderInfo={callback.orderInfo}&orderType={callback.orderType}&partnerCode={callback.partnerCode}&payType={callback.payType}&requestId={callback.requestId}&responseTime={callback.responseTime}&resultCode={callback.resultCode}&transId={callback.transId}";

                var signature = ComputeHmacSha256(rawSignature, config.SecretKey);

                if (signature.Equals(callback.signature))
                {
                    var callbackDto = new PaymentCallbackDto
                    {
                        TransactionId = callback.orderId,
                        Status = callback.resultCode == 0 ? "SUCCESS" : "FAILED", // SỬA: So sánh với int
                        ResponseCode = callback.resultCode.ToString(), // SỬA: Convert int sang string
                        Message = callback.message,
                        AdditionalData = new Dictionary<string, string>
                {
                    {"transId", callback.transId.ToString()},
                    {"payType", callback.payType}
                }
                    };

                    await _paymentService.ProcessCallbackAsync(callbackDto);

                    return Ok(new { message = "success" });
                }

                return BadRequest("Invalid signature");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MoMo callback error");
                return BadRequest("Error processing callback");
            }
        }

        private string ComputeHmacSha256(string message, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(messageBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}
*/