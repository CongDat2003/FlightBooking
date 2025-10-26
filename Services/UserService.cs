using FlightBooking.DTOs.User;
using FlightBooking.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace FlightBooking.Services
{
    public class UserService : IUserService
    {
        private readonly FlightBookingContext _context;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;

        public UserService(FlightBookingContext context, IEmailService emailService, INotificationService notificationService)
        {
            _context = context;
            _emailService = emailService;
            _notificationService = notificationService;
        }

        public async Task<UserProfileDto> RegisterAsync(RegisterUserDto registerDto)
        {
            // Check if username or email already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == registerDto.Username || u.Email == registerDto.Email);

            if (existingUser != null)
                throw new InvalidOperationException("Username or email already exists");

            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                FullName = registerDto.FullName,
                Phone = registerDto.Phone,
                DateOfBirth = registerDto.DateOfBirth,
                Gender = registerDto.Gender
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new UserProfileDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                CreatedAt = user.CreatedAt ?? DateTime.Now,
                TotalBookings = 0
            };
        }

        public async Task<UserProfileDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _context.Users
               .Include(u => u.Bookings)
               .FirstOrDefaultAsync(u =>
                   (u.Username == loginDto.UsernameOrEmail || u.Email == loginDto.UsernameOrEmail)
                   && (u.IsActive ?? true));

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
                throw new UnauthorizedAccessException("Invalid username or password");

            if (!(user.IsActive ?? true))
                throw new UnauthorizedAccessException("Account is deactivated");

            return new UserProfileDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                CreatedAt = user.CreatedAt ?? DateTime.Now,
                TotalBookings = user.Bookings.Count
            };
        }

        public async Task<UserProfileDto> GetProfileAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Bookings)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                throw new ArgumentException("User not found");

            return new UserProfileDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                CreatedAt = user.CreatedAt ?? DateTime.Now,
                TotalBookings = user.Bookings.Count
            };
        }

        public async Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileDto updateDto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new ArgumentException("User not found");

            if (!string.IsNullOrEmpty(updateDto.FullName))
                user.FullName = updateDto.FullName;
            if (updateDto.Phone != null)
                user.Phone = updateDto.Phone;
            if (updateDto.DateOfBirth.HasValue)
                user.DateOfBirth = updateDto.DateOfBirth;
            if (!string.IsNullOrEmpty(updateDto.Gender))
                user.Gender = updateDto.Gender;

            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return await GetProfileAsync(userId);
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto passwordDto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new ArgumentException("User not found");

            if (!BCrypt.Net.BCrypt.Verify(passwordDto.CurrentPassword, user.Password))
                throw new UnauthorizedAccessException("Current password is incorrect");

            user.Password = BCrypt.Net.BCrypt.HashPassword(passwordDto.NewPassword);
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<UserBookingHistoryDto>> GetBookingHistoryAsync(int userId, int page = 1, int pageSize = 10)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Flight)
                    .ThenInclude(f => f.Airline)
                .Include(b => b.Flight.DepartureAirport)
                .Include(b => b.Flight.ArrivalAirport)
                .Include(b => b.BookingSeats)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return bookings.Select(b => new UserBookingHistoryDto
            {
                BookingId = b.BookingId,
                BookingReference = b.BookingReference,
                FlightNumber = b.Flight.FlightNumber,
                AirlineName = b.Flight.Airline.AirlineName,
                Route = $"{b.Flight.DepartureAirport.AirportCode} → {b.Flight.ArrivalAirport.AirportCode}",
                DepartureTime = b.Flight.DepartureTime,
                ArrivalTime = b.Flight.ArrivalTime,
                BookingStatus = b.BookingStatus,
                PaymentStatus = b.PaymentStatus,
                TotalAmount = b.TotalAmount,
                BookingDate = b.BookingDate ?? DateTime.Now,
                PassengerCount = b.BookingSeats.Count,
                CanCancel = b.BookingStatus == "CONFIRMED" && b.Flight.DepartureTime > DateTime.Now.AddHours(24)
            }).ToList();
        }

        public async Task<BookingDetailDto> GetBookingDetailAsync(int userId, int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Flight)
                    .ThenInclude(f => f.Airline)
                .Include(b => b.Flight.DepartureAirport)
                .Include(b => b.Flight.ArrivalAirport)
                .Include(b => b.Flight.AircraftType)
                .Include(b => b.BookingSeats)
                    .ThenInclude(bs => bs.Seat)
                        .ThenInclude(s => s.Class)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.UserId == userId);

            if (booking == null)
                throw new ArgumentException("Booking not found");

            return new BookingDetailDto
            {
                BookingId = booking.BookingId,
                BookingReference = booking.BookingReference,
                BookingStatus = booking.BookingStatus,
                PaymentStatus = booking.PaymentStatus,
                TotalAmount = booking.TotalAmount,
                BookingDate = booking.BookingDate ?? DateTime.Now,
                Notes = booking.Notes,
                Flight = new FlightDetailDto
                {
                    FlightNumber = booking.Flight.FlightNumber,
                    AirlineName = booking.Flight.Airline.AirlineName,
                    AircraftModel = booking.Flight.AircraftType.AircraftModel,
                    DepartureAirport = $"{booking.Flight.DepartureAirport.AirportName} ({booking.Flight.DepartureAirport.AirportCode})",
                    ArrivalAirport = $"{booking.Flight.ArrivalAirport.AirportName} ({booking.Flight.ArrivalAirport.AirportCode})",
                    DepartureTime = booking.Flight.DepartureTime,
                    ArrivalTime = booking.Flight.ArrivalTime,
                    Gate = booking.Flight.Gate
                },
                Passengers = booking.BookingSeats.Select(bs => new PassengerSeatDto
                {
                    SeatNumber = bs.Seat.SeatNumber,
                    SeatClass = bs.Seat.Class.ClassName,
                    PassengerName = bs.PassengerName,
                    SeatPrice = bs.SeatPrice,
                    IsWindow = bs.Seat.IsWindow ?? false,
                    IsAisle = bs.Seat.IsAisle ?? false
                }).ToList()
            };
        }

        public async Task<bool> CancelBookingAsync(int userId, int bookingId)
        {
            var booking = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Flight)
                        .ThenInclude(f => f.DepartureAirport)
                    .Include(b => b.Flight)
                        .ThenInclude(f => f.ArrivalAirport)
                    .Include(b => b.BookingSeats)
                        .ThenInclude(bs => bs.Seat)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.UserId == userId);

            if (booking == null)
                throw new ArgumentException("Booking not found");

            if (booking.BookingStatus != "CONFIRMED")
                throw new InvalidOperationException("Only confirmed bookings can be cancelled");

            if (booking.Flight.DepartureTime <= DateTime.Now.AddHours(24))
                throw new InvalidOperationException("Cannot cancel booking less than 24 hours before departure");

            booking.BookingStatus = "CANCELLED";
            booking.PaymentStatus = "REFUNDED";

            // Release seats
            foreach (var bookingSeat in booking.BookingSeats)
            {
                bookingSeat.Seat.IsAvailable = true;
            }

            await _context.SaveChangesAsync();

            // Gửi notification hủy vé thành công
            await _notificationService.SendCancellationNotificationAsync(bookingId);

            return true;
        }

        public async Task<bool> DeleteAccountAsync(int userId, string password)
        {
            if (_context == null)
                throw new InvalidOperationException("Database context is null");

            var user = await _context.Users
                .Include(u => u.Bookings)
                    .ThenInclude(b => b.Flight)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                throw new ArgumentException("User not found");

            if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
                throw new UnauthorizedAccessException("Incorrect password");

            // Check if user has any active bookings
            var hasActiveBookings = user.Bookings != null && user.Bookings.Any(b => b.BookingStatus == "CONFIRMED" && b.Flight != null && b.Flight.DepartureTime > DateTime.Now);
            if (hasActiveBookings)
                throw new InvalidOperationException("Cannot delete account with active bookings");

            // Soft Delete: Set IsActive to false
            user.IsActive = false;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task SendPasswordResetOtpAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && (u.IsActive ?? true));
            if (user == null)
                throw new ArgumentException("Email không tồn tại hoặc tài khoản không hoạt động");

            // Vô hiệu hóa tất cả OTP chưa dùng, chưa hết hạn trước đó
            var existingTokens = await _context.PasswordResetTokens
                .Where(t => t.Email == email && !t.IsUsed && t.ExpirationTime > DateTime.Now)
                .ToListAsync();

            foreach (var token in existingTokens)
            {
                token.IsUsed = true; // Đánh dấu đã dùng để vô hiệu hóa
            }

            // Tạo mã OTP 6 chữ số ngẫu nhiên mới
            var otpCode = GenerateOtpCode(6);

            // Lưu OTP mới vào database với thời hạn 5 phút
            var expiration = DateTime.Now.AddMinutes(5);

            var resetToken = new PasswordResetToken
            {
                Email = email,
                OtpCode = otpCode,
                ExpirationTime = expiration,
                IsUsed = false
            };

            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            // Gửi email OTP
            await _emailService.SendPasswordResetOtpEmailAsync(email, otpCode, user.FullName);
        }

        public async Task<bool> VerifyPasswordResetOtpAsync(string email, string otpCode)
        {
            var token = await _context.PasswordResetTokens
                .Where(t => t.Email == email && t.OtpCode == otpCode && !t.IsUsed && t.ExpirationTime > DateTime.Now)
                .OrderByDescending(t => t.ExpirationTime)
                .FirstOrDefaultAsync();

            return token != null;
        }

        public async Task<bool> ResetPasswordAsync(string email, string otpCode, string newPassword)
        {
            var token = await _context.PasswordResetTokens
                .Where(t => t.Email == email && t.OtpCode == otpCode && !t.IsUsed && t.ExpirationTime > DateTime.Now)
                .OrderByDescending(t => t.ExpirationTime)
                .FirstOrDefaultAsync();

            if (token == null)
                throw new UnauthorizedAccessException("Mã xác thực không hợp lệ hoặc đã hết hạn");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && (u.IsActive ?? true));
            if (user == null)
                throw new ArgumentException("Người dùng không tồn tại");

            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.Now;

            token.IsUsed = true; // Đánh dấu OTP đã dùng

            await _context.SaveChangesAsync();
            return true;
        }

        private string GenerateOtpCode(int length)
        {
            // Sinh mã OTP gồm chữ số ngẫu nhiên
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[length];
            rng.GetBytes(bytes);
            var otp = new char[length];
            for (int i = 0; i < length; i++)
            {
                otp[i] = (char)('0' + (bytes[i] % 10));
            }
            return new string(otp);
        }
    }
}
