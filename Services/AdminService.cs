using FlightBooking.DTOs.Admin;
using FlightBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Services
{
    public class AdminService : IAdminService
    {
        private readonly FlightBookingContext _context;

        public AdminService(FlightBookingContext context)
        {
            _context = context;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            var today = DateTime.Today;
            var currentMonth = new DateTime(today.Year, today.Month, 1);

            var stats = new DashboardStatsDto
            {
                TotalFlights = await _context.Flights.CountAsync(),
                TotalBookings = await _context.Bookings.CountAsync(),
                TotalUsers = await _context.Users.CountAsync(),
                TotalRevenue = await _context.Bookings
                    .Where(b => b.PaymentStatus == "PAID")
                    .SumAsync(b => b.TotalAmount),
                TodayBookings = await _context.Bookings
                    .Where(b => b.BookingDate.HasValue && b.BookingDate.Value.Date == today)
                    .CountAsync(),
                TodayRevenue = await _context.Bookings
                    .Where(b => b.BookingDate.HasValue && b.BookingDate.Value.Date == today && b.PaymentStatus == "PAID")
                    .SumAsync(b => b.TotalAmount)
            };

            // Monthly revenue for last 12 months
            stats.MonthlyRevenue = await _context.Bookings
                .Where(b => b.PaymentStatus == "PAID" && b.BookingDate >= currentMonth.AddMonths(-11))
                .GroupBy(b => new {
                    Year = b.BookingDate.Value.Year,
                    Month = b.BookingDate.Value.Month
                })
                .Select(g => new RevenueByMonthDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(b => b.TotalAmount),
                    BookingCount = g.Count()
                })
                .OrderBy(r => r.Year).ThenBy(r => r.Month)
                .ToListAsync();

            // Popular routes
            stats.PopularRoutes = await _context.Bookings
                .Include(b => b.Flight)
                    .ThenInclude(f => f.DepartureAirport)
                .Include(b => b.Flight)
                    .ThenInclude(f => f.ArrivalAirport)
                .Where(b => b.BookingStatus == "CONFIRMED")
                .GroupBy(b => new {
                    DepartureCode = b.Flight.DepartureAirport.AirportCode,
                    ArrivalCode = b.Flight.ArrivalAirport.AirportCode
                })
                .Select(g => new PopularRouteDto
                {
                    Route = $"{g.Key.DepartureCode} → {g.Key.ArrivalCode}",
                    BookingCount = g.Count(),
                    Revenue = g.Where(b => b.PaymentStatus == "PAID").Sum(b => b.TotalAmount)
                })
                .OrderByDescending(r => r.BookingCount)
                .Take(10)
                .ToListAsync();

            // Booking status stats
            var totalBookings = stats.TotalBookings;
            stats.BookingStats = await _context.Bookings
                .GroupBy(b => b.BookingStatus)
                .Select(g => new BookingStatusStatsDto
                {
                    Status = g.Key,
                    Count = g.Count(),
                    Percentage = totalBookings > 0 ? (decimal)g.Count() / totalBookings * 100 : 0
                })
                .ToListAsync();

            return stats;
        }

        public async Task<AdminFlightResponseDto> CreateFlightAsync(CreateFlightDto flightDto)
        {
            var flight = new Flight
            {
                FlightNumber = flightDto.FlightNumber,
                AirlineId = flightDto.AirlineId,
                AircraftTypeId = flightDto.AircraftTypeId,
                DepartureAirportId = flightDto.DepartureAirportId,
                ArrivalAirportId = flightDto.ArrivalAirportId,
                DepartureTime = flightDto.DepartureTime,
                ArrivalTime = flightDto.ArrivalTime,
                BasePrice = flightDto.BasePrice,
                Gate = flightDto.Gate
            };

            _context.Flights.Add(flight);
            await _context.SaveChangesAsync();

            // Generate seats for the flight
            await GenerateSeatsForFlightAsync(flight.FlightId);

            return await GetFlightByIdAsync(flight.FlightId);
        }

        public async Task<bool> GenerateSeatsForFlightAsync(int flightId)
        {
            var flight = await _context.Flights
                .Include(f => f.AircraftType)
                .FirstOrDefaultAsync(f => f.FlightId == flightId);

            if (flight == null) return false;

            var seatClasses = await _context.SeatClasses.ToListAsync();
            var economyClass = seatClasses.First(sc => sc.ClassName == "ECONOMY");
            var businessClass = seatClasses.First(sc => sc.ClassName == "BUSINESS");
            var firstClass = seatClasses.First(sc => sc.ClassName == "FIRST_CLASS");

            var seats = new List<Seat>();
            var columns = new[] { "A", "B", "C", "D", "E", "F" };

            // Generate First Class seats (rows 1-2)
            for (int row = 1; row <= 2 && seats.Count < flight.AircraftType.FirstClassSeats; row++)
            {
                for (int col = 0; col < columns.Length && seats.Count < flight.AircraftType.FirstClassSeats; col++)
                {
                    seats.Add(new Seat
                    {
                        FlightId = flightId,
                        SeatNumber = $"{row}{columns[col]}",
                        SeatRow = row,
                        SeatColumn = columns[col],
                        ClassId = firstClass.ClassId,
                        IsWindow = col == 0 || col == columns.Length - 1,
                        IsAisle = col == 2 || col == 3
                    });
                }
            }

            // Generate Business Class seats (rows 3-8)
            for (int row = 3; row <= 8 && seats.Count < flight.AircraftType.FirstClassSeats + flight.AircraftType.BusinessSeats; row++)
            {
                for (int col = 0; col < columns.Length && seats.Count < flight.AircraftType.FirstClassSeats + flight.AircraftType.BusinessSeats; col++)
                {
                    seats.Add(new Seat
                    {
                        FlightId = flightId,
                        SeatNumber = $"{row}{columns[col]}",
                        SeatRow = row,
                        SeatColumn = columns[col],
                        ClassId = businessClass.ClassId,
                        IsWindow = col == 0 || col == columns.Length - 1,
                        IsAisle = col == 2 || col == 3
                    });
                }
            }

            // Generate Economy Class seats (remaining rows)
            int startRow = 9;
            int currentSeatCount = seats.Count;
            for (int row = startRow; currentSeatCount < flight.AircraftType.TotalSeats; row++)
            {
                for (int col = 0; col < columns.Length && currentSeatCount < flight.AircraftType.TotalSeats; col++)
                {
                    seats.Add(new Seat
                    {
                        FlightId = flightId,
                        SeatNumber = $"{row}{columns[col]}",
                        SeatRow = row,
                        SeatColumn = columns[col],
                        ClassId = economyClass.ClassId,
                        IsWindow = col == 0 || col == columns.Length - 1,
                        IsAisle = col == 2 || col == 3,
                        IsEmergencyExit = row == 12 || row == 13 // Emergency exit rows
                    });
                    currentSeatCount++;
                }
            }

            _context.Seats.AddRange(seats);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<AdminFlightResponseDto>> GetAllFlightsAsync(int page = 1, int pageSize = 10)
        {
            var flights = await _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.AircraftType)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .Include(f => f.Seats)
                .Include(f => f.Bookings.Where(b => b.PaymentStatus == "PAID"))
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return flights.Select(f => new AdminFlightResponseDto
            {
                FlightId = f.FlightId,
                FlightNumber = f.FlightNumber,
                AirlineName = f.Airline.AirlineName,
                AircraftModel = f.AircraftType.AircraftModel,
                DepartureAirport = $"{f.DepartureAirport.AirportName} ({f.DepartureAirport.AirportCode})",
                ArrivalAirport = $"{f.ArrivalAirport.AirportName} ({f.ArrivalAirport.AirportCode})",
                DepartureTime = f.DepartureTime,
                ArrivalTime = f.ArrivalTime,
                BasePrice = f.BasePrice,
                Status = f.Status,
                Gate = f.Gate,
                TotalSeats = f.Seats.Count,
                BookedSeats = f.Seats.Count(s => s.IsAvailable.HasValue && !s.IsAvailable.Value),
                AvailableSeats = f.Seats.Count(s => s.IsAvailable.HasValue && s.IsAvailable.Value),
                Revenue = f.Bookings.Sum(b => b.TotalAmount),
                CreatedAt = f.CreatedAt.HasValue ? f.CreatedAt.Value : DateTime.Now
            }).ToList();
        }

        public async Task<AdminFlightResponseDto> GetFlightByIdAsync(int flightId)
        {
            var flight = await _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.AircraftType)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .Include(f => f.Seats)
                .Include(f => f.Bookings.Where(b => b.PaymentStatus == "PAID"))
                .FirstOrDefaultAsync(f => f.FlightId == flightId);

            if (flight == null)
                throw new ArgumentException("Flight not found");

            return new AdminFlightResponseDto
            {
                FlightId = flight.FlightId,
                FlightNumber = flight.FlightNumber,
                AirlineName = flight.Airline.AirlineName,
                AircraftModel = flight.AircraftType.AircraftModel,
                DepartureAirport = $"{flight.DepartureAirport.AirportName} ({flight.DepartureAirport.AirportCode})",
                ArrivalAirport = $"{flight.ArrivalAirport.AirportName} ({flight.ArrivalAirport.AirportCode})",
                DepartureTime = flight.DepartureTime,
                ArrivalTime = flight.ArrivalTime,
                BasePrice = flight.BasePrice,
                Status = flight.Status,
                Gate = flight.Gate,
                TotalSeats = flight.Seats.Count,
                BookedSeats = flight.Seats.Count(s => s.IsAvailable.HasValue && !s.IsAvailable.Value),
                AvailableSeats = flight.Seats.Count(s => s.IsAvailable.HasValue && s.IsAvailable.Value),
                Revenue = flight.Bookings.Sum(b => b.TotalAmount),
                CreatedAt = flight.CreatedAt.HasValue ? flight.CreatedAt.Value : DateTime.Now
            };
        }

        // Implement other admin methods...
        public async Task<AdminFlightResponseDto> UpdateFlightAsync(int flightId, UpdateFlightDto flightDto)
        {
            var flight = await _context.Flights.FindAsync(flightId);
            if (flight == null)
                throw new ArgumentException("Flight not found");

            if (!string.IsNullOrEmpty(flightDto.FlightNumber))
                flight.FlightNumber = flightDto.FlightNumber;
            if (flightDto.DepartureTime.HasValue)
                flight.DepartureTime = flightDto.DepartureTime.Value;
            if (flightDto.ArrivalTime.HasValue)
                flight.ArrivalTime = flightDto.ArrivalTime.Value;
            if (flightDto.BasePrice.HasValue)
                flight.BasePrice = flightDto.BasePrice.Value;
            if (!string.IsNullOrEmpty(flightDto.Status))
                flight.Status = flightDto.Status;
            if (flightDto.Gate != null)
                flight.Gate = flightDto.Gate;

            /*var now = DateTime.Now;

            if (flight.DepartureTime != null && flight.ArrivalTime != null)
            {
                if (now >= flight.ArrivalTime)
                {
                    // Đã đến hoặc vượt quá giờ hạ cánh
                    flight.Status = "COMPLETED";
                }
                else if (now >= flight.DepartureTime)
                {
                    // Đã đến hoặc vượt quá giờ khởi hành nhưng chưa đến giờ hạ cánh
                    flight.Status = "DEPARTED";
                }
                else if (now >= flight.DepartureTime.AddMinutes(-5) && now < flight.DepartureTime)
                {
                    // Trong khoảng 5 phút trước giờ khởi hành
                    flight.Status = "PREPARING";
                }
                else
                {
                    // Trước khoảng 5 phút khởi hành, giữ nguyên trạng thái hoặc có thể là "SCHEDULED"
                    if (string.IsNullOrEmpty(flight.Status) || flight.Status == "PREPARING" || flight.Status == "DEPARTED" || flight.Status == "COMPLETED")
                    {
                        flight.Status = "SCHEDULED";
                    }
                }
            }*/

            await _context.SaveChangesAsync();
            return await GetFlightByIdAsync(flightId);
        }

        public async Task<bool> DeleteFlightAsync(int flightId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var flight = await _context.Flights
                    .Include(f => f.Bookings)
                    .Include(f => f.Seats)
                        .ThenInclude(s => s.BookingSeats) // Include booking seats
                    .FirstOrDefaultAsync(f => f.FlightId == flightId);

                if (flight == null) return false;

                // Kiểm tra có booking confirmed không
                if (flight.Bookings.Any(b => b.BookingStatus == "CONFIRMED"))
                    throw new InvalidOperationException("Cannot delete flight with confirmed bookings");

                // Kiểm tra có ghế đã được đặt không
                if (flight.Seats.Any(s => s.BookingSeats.Any()))
                    throw new InvalidOperationException("Cannot delete flight with booked seats");

                // Xóa tất cả seats trước
                if (flight.Seats.Any())
                {
                    _context.Seats.RemoveRange(flight.Seats);
                }

                // Xóa flight
                _context.Flights.Remove(flight);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<AdminBookingResponseDto>> GetAllBookingsAsync(int page = 1, int pageSize = 10)
        {
            var bookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Flight)
                    .ThenInclude(f => f.DepartureAirport)
                .Include(b => b.Flight)
                    .ThenInclude(f => f.ArrivalAirport)
                .Include(b => b.BookingSeats)
                    .ThenInclude(bs => bs.Seat)
                        .ThenInclude(s => s.Class)
                .OrderByDescending(b => b.BookingDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return bookings.Select(b => new AdminBookingResponseDto
            {
                BookingId = b.BookingId,
                BookingReference = b.BookingReference,
                UserName = b.User.FullName,
                UserEmail = b.User.Email,
                FlightNumber = b.Flight.FlightNumber,
                Route = $"{b.Flight.DepartureAirport.AirportCode} → {b.Flight.ArrivalAirport.AirportCode}",
                FlightDate = b.Flight.DepartureTime,
                BookingStatus = b.BookingStatus,
                PaymentStatus = b.PaymentStatus,
                TotalAmount = b.TotalAmount,
                BookingDate = b.BookingDate.HasValue ? b.BookingDate.Value : DateTime.Now,
                PassengerCount = b.BookingSeats.Count,
                Seats = b.BookingSeats.Select(bs => new AdminBookingSeatDto
                {
                    SeatNumber = bs.Seat.SeatNumber,
                    SeatClass = bs.Seat.Class.ClassName,
                    PassengerName = bs.PassengerName,
                    PassengerIdNumber = bs.PassengerIdNumber,
                    SeatPrice = bs.SeatPrice
                }).ToList()
            }).ToList();
        }

        public async Task<AdminBookingResponseDto> UpdateBookingStatusAsync(int bookingId, UpdateBookingStatusDto statusDto)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
                throw new ArgumentException("Booking not found");

            booking.BookingStatus = statusDto.BookingStatus;
            if (!string.IsNullOrEmpty(statusDto.PaymentStatus))
                booking.PaymentStatus = statusDto.PaymentStatus;
            if (!string.IsNullOrEmpty(statusDto.Notes))
                booking.Notes = statusDto.Notes;

            await _context.SaveChangesAsync();
            return await GetBookingByIdAsync(bookingId);
        }

        public async Task<AdminBookingResponseDto> GetBookingByIdAsync(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Flight)
                    .ThenInclude(f => f.DepartureAirport)
                .Include(b => b.Flight)
                    .ThenInclude(f => f.ArrivalAirport)
                .Include(b => b.BookingSeats)
                    .ThenInclude(bs => bs.Seat)
                        .ThenInclude(s => s.Class)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
                throw new ArgumentException("Booking not found");

            return new AdminBookingResponseDto
            {
                BookingId = booking.BookingId,
                BookingReference = booking.BookingReference,
                UserName = booking.User.FullName,
                UserEmail = booking.User.Email,
                FlightNumber = booking.Flight.FlightNumber,
                Route = $"{booking.Flight.DepartureAirport.AirportCode} → {booking.Flight.ArrivalAirport.AirportCode}",
                FlightDate = booking.Flight.DepartureTime,
                BookingStatus = booking.BookingStatus,
                PaymentStatus = booking.PaymentStatus,
                TotalAmount = booking.TotalAmount,
                BookingDate = booking.BookingDate.HasValue ? booking.BookingDate.Value : DateTime.Now,
                PassengerCount = booking.BookingSeats.Count,
                Seats = booking.BookingSeats.Select(bs => new AdminBookingSeatDto
                {
                    SeatNumber = bs.Seat.SeatNumber,
                    SeatClass = bs.Seat.Class.ClassName,
                    PassengerName = bs.PassengerName,
                    PassengerIdNumber = bs.PassengerIdNumber,
                    SeatPrice = bs.SeatPrice
                }).ToList()
            };
        }

        public async Task<bool> CancelBookingAsync(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingSeats)
                    .ThenInclude(bs => bs.Seat)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) return false;

            // Update booking status
            booking.BookingStatus = "CANCELLED";
            booking.PaymentStatus = "REFUNDED";

            // Release seats
            foreach (var bookingSeat in booking.BookingSeats)
            {
                bookingSeat.Seat.IsAvailable = true;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<AdminUserResponseDto>> GetAllUsersAsync(int page = 1, int pageSize = 10)
        {
            var users = await _context.Users
                .Include(u => u.Bookings.Where(b => b.PaymentStatus == "PAID"))
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return users.Select(u => new AdminUserResponseDto
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email,
                FullName = u.FullName,
                Phone = u.Phone,
                DateOfBirth = u.DateOfBirth,
                Gender = u.Gender,
                IsActive = u.IsActive.HasValue ? u.IsActive.Value : true,
                CreatedAt = u.CreatedAt.HasValue ? u.CreatedAt.Value : DateTime.Now,
                UpdatedAt = u.UpdatedAt.HasValue ? u.UpdatedAt.Value : DateTime.Now,
                TotalBookings = u.Bookings.Count,
                TotalSpent = u.Bookings.Sum(b => b.TotalAmount)
            }).ToList();
        }

        public async Task<AdminUserResponseDto> GetUserByIdAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Bookings.Where(b => b.PaymentStatus == "PAID"))
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                throw new ArgumentException("User not found");

            return new AdminUserResponseDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                IsActive = user.IsActive.HasValue ? user.IsActive.Value : true, 
                CreatedAt = user.CreatedAt.HasValue ? user.CreatedAt.Value : DateTime.Now, 
                UpdatedAt = user.UpdatedAt.HasValue ? user.UpdatedAt.Value : DateTime.Now, 
                TotalBookings = user.Bookings.Count,
                TotalSpent = user.Bookings.Sum(b => b.TotalAmount)
            };
        }

        public async Task<AdminUserResponseDto> UpdateUserStatusAsync(int userId, UpdateUserStatusDto statusDto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new ArgumentException("User not found");

            user.IsActive = statusDto.IsActive;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return await GetUserByIdAsync(userId);
        }

        public async Task<List<RevenueByMonthDto>> GetRevenueReportAsync(int year)
        {
            return await _context.Bookings
                .Where(b => b.PaymentStatus == "PAID" &&
                           b.BookingDate.HasValue &&
                           b.BookingDate.Value.Year == year)
                .GroupBy(b => b.BookingDate.Value.Month)
                .Select(g => new RevenueByMonthDto
                {
                    Year = year,
                    Month = g.Key,
                    Revenue = g.Sum(b => b.TotalAmount),
                    BookingCount = g.Count()
                })
                .OrderBy(r => r.Month)
                .ToListAsync();
        }

        public async Task<List<RevenueByMonthDto>> GetRevenueReportAsync(int startYear, int endYear)
        {
            return await _context.Bookings
                .Where(b => b.PaymentStatus == "PAID" &&
                           b.BookingDate.HasValue &&
                           b.BookingDate.Value.Year >= startYear &&
                           b.BookingDate.Value.Year <= endYear)
                .GroupBy(b => new { b.BookingDate.Value.Year, b.BookingDate.Value.Month })
                .Select(g => new RevenueByMonthDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(b => b.TotalAmount),
                    BookingCount = g.Count()
                })
                .OrderBy(r => r.Year).ThenBy(r => r.Month)
                .ToListAsync();
        }

        public async Task<List<PopularRouteDto>> GetPopularRoutesAsync(int topCount = 10)
        {
            return await _context.Bookings
                .Include(b => b.Flight)
                    .ThenInclude(f => f.DepartureAirport)
                .Include(b => b.Flight)
                    .ThenInclude(f => f.ArrivalAirport)
                .Where(b => b.BookingStatus == "CONFIRMED")
                .GroupBy(b => new {
                    DepartureCode = b.Flight.DepartureAirport.AirportCode,
                    ArrivalCode = b.Flight.ArrivalAirport.AirportCode
                })
                .Select(g => new PopularRouteDto
                {
                    Route = $"{g.Key.DepartureCode} → {g.Key.ArrivalCode}",
                    BookingCount = g.Count(),
                    Revenue = g.Where(b => b.PaymentStatus == "PAID").Sum(b => b.TotalAmount)
                })
                .OrderByDescending(r => r.BookingCount)
                .Take(topCount)
                .ToListAsync();
        }
    }
}
