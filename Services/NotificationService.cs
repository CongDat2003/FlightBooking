using FlightBooking.DTOs;
using FlightBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Services
{
    public class NotificationService : INotificationService
    {
        private readonly FlightBookingContext _context;
        private readonly IEmailService _emailService;
        private readonly ISMSService _smsService;
        private readonly IPushNotificationService _pushService;

        public NotificationService(
            FlightBookingContext context,
            IEmailService emailService,
            ISMSService smsService,
            IPushNotificationService pushService)
        {
            _context = context;
            _emailService = emailService;
            _smsService = smsService;
            _pushService = pushService;
        }

        public async Task SendBookingConfirmationAsync(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Flight)
                    .ThenInclude(f => f.Airline)
                .Include(b => b.Flight.DepartureAirport)
                .Include(b => b.Flight.ArrivalAirport)
                .Include(b => b.BookingSeats)
                    .ThenInclude(bs => bs.Seat)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) return;

            var notification = new Notification
            {
                UserId = booking.UserId,
                Title = "Đặt vé thành công",
                Message = $"Đặt vé thành công cho chuyến bay {booking.Flight.FlightNumber}. Mã đặt chỗ: {booking.BookingReference}",
                Type = "BOOKING",
                RelatedBookingId = bookingId
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send email
            await _emailService.SendBookingConfirmationEmailAsync(booking);

            // Send SMS
            if (!string.IsNullOrEmpty(booking.User.Phone))
            {
                await _smsService.SendBookingConfirmationSMSAsync(booking.User.Phone, booking.BookingReference);
            }

            // Send push notification
            await _pushService.SendAsync(booking.UserId, notification.Title, notification.Message);
        }

        public async Task SendFlightReminderAsync(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Flight)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) return;

            var notification = new Notification
            {
                UserId = booking.UserId,
                Title = "Nhắc nhở chuyến bay",
                Message = $"Chuyến bay {booking.Flight.FlightNumber} sẽ khởi hành trong 24 giờ tới. Vui lòng có mặt tại sân bay trước 2 giờ.",
                Type = "REMINDER",
                RelatedBookingId = bookingId
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send push notification
            await _pushService.SendAsync(booking.UserId, notification.Title, notification.Message);

            // Send SMS
            if (!string.IsNullOrEmpty(booking.User.Phone))
            {
                await _smsService.SendAsync(booking.User.Phone, notification.Message);
            }
        }

        public async Task SendFlightUpdateAsync(int flightId, string updateType, string message)
        {
            var bookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Flight)
                .Where(b => b.FlightId == flightId && b.BookingStatus == "CONFIRMED")
                .ToListAsync();

            foreach (var booking in bookings)
            {
                var notification = new Notification
                {
                    UserId = booking.UserId,
                    Title = $"Cập nhật chuyến bay {booking.Flight.FlightNumber}",
                    Message = message,
                    Type = "FLIGHT_UPDATE",
                    RelatedBookingId = booking.BookingId,
                    RelatedFlightId = flightId,
                    AdditionalData = System.Text.Json.JsonSerializer.Serialize(new { UpdateType = updateType })
                };

                _context.Notifications.Add(notification);

                // Send push notification immediately
                await _pushService.SendAsync(booking.UserId, notification.Title, notification.Message);

                // Send SMS for critical updates
                if (updateType == "CANCELLED" || updateType == "DELAYED")
                {
                    if (!string.IsNullOrEmpty(booking.User.Phone))
                    {
                        await _smsService.SendFlightUpdateSMSAsync(booking.User.Phone, booking.Flight.FlightNumber, message);
                    }
                }

                // Send email
                await _emailService.SendFlightUpdateEmailAsync(booking.User.Email, notification.Title, message);
            }

            await _context.SaveChangesAsync();
        }

        public async Task SendCancellationNotificationAsync(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Flight)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) return;

            var notification = new Notification
            {
                UserId = booking.UserId,
                Title = "Hủy đặt vé thành công",
                Message = $"Đặt vé {booking.BookingReference} đã được hủy thành công. Số tiền sẽ được hoàn lại trong 3-5 ngày làm việc.",
                Type = "CANCELLATION",
                RelatedBookingId = bookingId
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send email
            await _emailService.SendCancellationEmailAsync(booking);

            // Send push notification
            await _pushService.SendAsync(booking.UserId, notification.Title, notification.Message);
        }

        public async Task SendPaymentConfirmationAsync(int paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.User)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null) return;

            var notification = new Notification
            {
                UserId = payment.Booking.UserId,
                Title = "Thanh toán thành công",
                Message = $"Thanh toán cho đặt vé {payment.Booking.BookingReference} đã được xử lý thành công. Số tiền: {payment.Amount:N0} VND",
                Type = "PAYMENT",
                RelatedBookingId = payment.BookingId
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send email
            await _emailService.SendPaymentConfirmationEmailAsync(payment);

            // Send push notification
            await _pushService.SendAsync(payment.Booking.UserId, notification.Title, notification.Message);
        }

        public async Task<List<NotificationDto>> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 20)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return notifications.Select(n => new NotificationDto
            {
                NotificationId = n.NotificationId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                Status = n.Status,
                CreatedAt = n.CreatedAt,
                ReadAt = n.ReadAt,
                RelatedBookingId = n.RelatedBookingId,
                RelatedFlightId = n.RelatedFlightId
            }).ToList();
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

            if (notification == null) return false;

            notification.Status = "READ";
            notification.ReadAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && n.Status == "UNREAD");
        }
    }
}
