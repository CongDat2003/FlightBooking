using FlightBooking.Configuration;
using FlightBooking.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FlightBooking.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendBookingConfirmationEmailAsync(Booking booking)
        {
            var subject = $"✈️ Xác nhận đặt vé - {booking.BookingReference}";
            var body = GenerateBookingConfirmationHtml(booking);

            await SendEmailAsync(booking.User.Email, subject, body, true);
        }

        public async Task SendFlightUpdateEmailAsync(string email, string subject, string message)
        {
            var body = GenerateFlightUpdateHtml(subject, message);
            await SendEmailAsync(email, $"🔔 {subject}", body, true);
        }

        public async Task SendCancellationEmailAsync(Booking booking)
        {
            var subject = $"❌ Hủy đặt vé - {booking.BookingReference}";
            var body = GenerateCancellationHtml(booking);

            await SendEmailAsync(booking.User.Email, subject, body, true);
        }

        public async Task SendPaymentConfirmationEmailAsync(Payment payment)
        {
            var subject = $"💳 Xác nhận thanh toán - {payment.TransactionId}";
            var body = GeneratePaymentConfirmationHtml(payment);

            await SendEmailAsync(payment.Booking.User.Email, subject, body, true);
        }

        private async Task SendEmailAsync(string email, string subject, string body, bool isHtml = false)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                message.To.Add(new MailboxAddress("", email));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder();
                if (isHtml)
                {
                    bodyBuilder.HtmlBody = body;
                }
                else
                {
                    bodyBuilder.TextBody = body;
                }
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();

                // Connect to SMTP server
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort,
                    _emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

                // Authenticate
                await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);

                // Send email
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation($"Email sent successfully to {email} - Subject: {subject}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {email}. Subject: {subject}");
                throw;
            }
        }

        private string GenerateBookingConfirmationHtml(Booking booking)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
        .booking-info {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .flight-details {{ display: flex; justify-content: space-between; align-items: center; margin: 15px 0; }}
        .airport {{ text-align: center; flex: 1; }}
        .arrow {{ flex: 0 0 50px; text-align: center; font-size: 24px; color: #667eea; }}
        .highlight {{ color: #667eea; font-weight: bold; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 14px; }}
        .qr-placeholder {{ width: 100px; height: 100px; background: #e9ecef; border: 2px dashed #adb5bd; margin: 20px auto; display: flex; align-items: center; justify-content: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✈️ Đặt vé thành công!</h1>
            <p>Cảm ơn bạn đã tin tưởng dịch vụ của chúng tôi</p>
        </div>
        
        <div class='content'>
            <h2>Xin chào <span class='highlight'>{booking.User.FullName}</span>!</h2>
            <p>Đặt vé của bạn đã được xác nhận thành công. Dưới đây là thông tin chi tiết:</p>
            
            <div class='booking-info'>
                <h3>📋 Thông tin đặt chỗ</h3>
                <p><strong>Mã đặt chỗ:</strong> <span class='highlight'>{booking.BookingReference}</span></p>
                <p><strong>Chuyến bay:</strong> {booking.Flight.FlightNumber}</p>
                <p><strong>Hãng bay:</strong> {booking.Flight.Airline.AirlineName}</p>
                
                <div class='flight-details'>
                    <div class='airport'>
                        <h4>{booking.Flight.DepartureAirport.AirportCode}</h4>
                        <p>{booking.Flight.DepartureAirport.AirportName}</p>
                        <p><strong>{booking.Flight.DepartureTime:dd/MM/yyyy}</strong></p>
                        <p><strong>{booking.Flight.DepartureTime:HH:mm}</strong></p>
                    </div>
                    <div class='arrow'>✈️</div>
                    <div class='airport'>
                        <h4>{booking.Flight.ArrivalAirport.AirportCode}</h4>
                        <p>{booking.Flight.ArrivalAirport.AirportName}</p>
                        <p><strong>{booking.Flight.ArrivalTime:dd/MM/yyyy}</strong></p>
                        <p><strong>{booking.Flight.ArrivalTime:HH:mm}</strong></p>
                    </div>
                </div>
                
                <p><strong>💰 Tổng tiền:</strong> <span class='highlight'>{booking.TotalAmount:N0} VND</span></p>
                <p><strong>🎫 Số ghế:</strong> {string.Join(", ", booking.BookingSeats.Select(bs => bs.Seat.SeatNumber))}</p>
                
                <div class='qr-placeholder'>
                    <span>QR Code</span>
                </div>
            </div>
            
            <div style='background: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                <h4>📝 Lưu ý quan trọng:</h4>
                <ul>
                    <li>Vui lòng có mặt tại sân bay trước <strong>2 giờ</strong> (chuyến bay nội địa) hoặc <strong>3 giờ</strong> (chuyến bay quốc tế)</li>
                    <li>Mang theo giấy tờ tùy thân hợp lệ</li>
                    <li>Kiểm tra hành lý theo quy định của hãng bay</li>
                    <li>Lưu mã đặt chỗ để check-in online</li>
                </ul>
            </div>
        </div>
        
        <div class='footer'>
            <p>Chúc bạn có chuyến bay an toàn và vui vẻ! 🛫</p>
            <p>Đội ngũ Flight Booking System</p>
            <p style='font-size: 12px; color: #999;'>Email này được gửi tự động, vui lòng không reply.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateFlightUpdateHtml(string title, string message)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #f39c12; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
        .alert {{ background: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔔 {title}</h1>
        </div>
        <div class='content'>
            <div class='alert'>
                <p>{message}</p>
            </div>
            <p>Vui lòng kiểm tra thông tin mới nhất trên ứng dụng hoặc liên hệ hotline để được hỗ trợ.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateCancellationHtml(Booking booking)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #e74c3c; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
        .refund-info {{ background: #d4edda; border: 1px solid #c3e6cb; padding: 15px; border-radius: 5px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>❌ Hủy đặt vé thành công</h1>
        </div>
        <div class='content'>
            <h2>Xin chào {booking.User?.FullName ?? "Quý khách"}!</h2>
            <p>Đặt vé của bạn đã được hủy thành công:</p>
            
            <ul>
                <li><strong>Mã đặt chỗ:</strong> {booking.BookingReference}</li>
                <li><strong>Chuyến bay:</strong> {booking.Flight?.FlightNumber ?? "N/A"}</li>
                <li><strong>Tuyến bay:</strong> {booking.Flight?.DepartureAirport?.AirportCode ?? "N/A"} → {booking.Flight?.ArrivalAirport?.AirportCode ?? "N/A"}</li>
            </ul>
            
            <div class='refund-info'>
                <h4>💰 Thông tin hoàn tiền:</h4>
                <p><strong>Số tiền hoàn:</strong> {booking.TotalAmount:N0} VND</p>
                <p><strong>Thời gian hoàn tiền:</strong> 3-5 ngày làm việc</p>
                <p>Tiền sẽ được hoàn về phương thức thanh toán ban đầu.</p>
            </div>
        </div>
    </div>
</body>
</html>";
        }

        private string GeneratePaymentConfirmationHtml(Payment payment)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #27ae60; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
        .payment-details {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>💳 Thanh toán thành công!</h1>
        </div>
        <div class='content'>
            <p>Thanh toán của bạn đã được xử lý thành công.</p>
            
            <div class='payment-details'>
                <h3>Chi tiết giao dịch:</h3>
                <ul>
                    <li><strong>Mã giao dịch:</strong> {payment.TransactionId}</li>
                    <li><strong>Số tiền:</strong> {payment.Amount:N0} VND</li>
                    <li><strong>Phương thức:</strong> {payment.PaymentMethod}</li>
                    <li><strong>Thời gian:</strong> {payment.ProcessedAt:dd/MM/yyyy HH:mm}</li>
                    <li><strong>Mã đặt chỗ:</strong> {payment.Booking.BookingReference}</li>
                </ul>
            </div>
            
            <p>Vé của bạn đã được xác nhận. Chúc bạn có chuyến bay vui vẻ!</p>
        </div>
    </div>
</body>
</html>";
        }

        public async Task SendPasswordResetOtpEmailAsync(string recipientEmail, string otpCode, string recipientName)
        {
            var subject = "✈️ Mã xác thực đặt lại mật khẩu của bạn";
            var body = GeneratePasswordResetOtpHtml(otpCode, recipientName);
            await SendEmailAsync(recipientEmail, subject, body, true);
        }

        private string GeneratePasswordResetOtpHtml(string otpCode, string recipientName)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #f0ad4e 0%, #eb9316 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
        .otp-box {{ background: white; padding: 25px; border-radius: 8px; margin: 25px 0; text-align: center; box-shadow: 0 4px 8px rgba(0,0,0,0.1); }}
        .otp-code {{ font-size: 48px; font-weight: bold; color: #f0ad4e; letter-spacing: 5px; }}
        .message {{ font-size: 16px; margin-bottom: 20px; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 14px; }}
        .disclaimer {{ font-size: 12px; color: #999; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔑 Yêu cầu đặt lại mật khẩu</h1>
            <p>Mã xác thực của bạn cho Flight Booking System</p>
        </div>
        
        <div class='content'>
            <h2>Xin chào {(string.IsNullOrEmpty(recipientName) ? "bạn" : recipientName)},</h2>
            <p class='message'>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. Vui lòng sử dụng mã xác thực dưới đây để hoàn tất quá trình:</p>
            
            <div class='otp-box'>
                <p>Mã xác thực của bạn là:</p>
                <div class='otp-code'>{otpCode}</div>
            </div>
            
            <p>Mã này sẽ hết hạn sau <strong>5 phút</strong>. Vui lòng không chia sẻ mã này với bất kỳ ai.</p>
            <p>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email này.</p>
        </div>
        
        <div class='footer'>
            <p>Trân trọng,</p>
            <p>Đội ngũ Flight Booking System</p>
            <p class='disclaimer'>Email này được gửi tự động. Vui lòng không trả lời email này.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
