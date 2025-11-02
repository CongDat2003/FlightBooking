using FlightBooking.DTOs;
using FlightBooking.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlightBooking.Services
{
    public class FlightService : IFlightService
    {
        private readonly FlightBookingContext _context;
        private readonly INotificationService _notificationService;

        public FlightService(FlightBookingContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<List<FlightResponseDto>> SearchFlightsAsync(FlightSearchDto searchDto)
        {
            var query = _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .Include(f => f.Seats)
                .Where(f => f.DepartureAirport.AirportCode == searchDto.DepartureAirportCode
                         && f.ArrivalAirport.AirportCode == searchDto.ArrivalAirportCode
                         && f.DepartureTime.Date == searchDto.DepartureDate.Date);
                // Removed status filter to return all flights for categorization in UI

            // Filter by aircraft type
            if (searchDto.AircraftTypeId.HasValue)
            {
                query = query.Where(f => f.AircraftTypeId == searchDto.AircraftTypeId.Value);
            }

            // Filter by departure time range (hour:minute in the day)
            if (searchDto.DepartureTimeFrom.HasValue)
            {
                var fromTime = searchDto.DepartureTimeFrom.Value;
                query = query.Where(f => f.DepartureTime.TimeOfDay >= fromTime);
            }
            if (searchDto.DepartureTimeTo.HasValue)
            {
                var toTime = searchDto.DepartureTimeTo.Value;
                query = query.Where(f => f.DepartureTime.TimeOfDay <= toTime);
            }

            // Filter by seat class
            if (!string.IsNullOrEmpty(searchDto.SeatClass))
            {
                query = query.Where(f => f.Seats.Any(s => s.Class.ClassName == searchDto.SeatClass && s.IsAvailable == true));
            }

            var flights = await query.ToListAsync();

            return flights.Select(f => new FlightResponseDto
            {
                FlightId = f.FlightId,
                FlightNumber = f.FlightNumber,
                AirlineName = f.Airline.AirlineName,
                AirlineCode = f.Airline.AirlineCode,
                DepartureAirport = $"{f.DepartureAirport.AirportName} ({f.DepartureAirport.AirportCode})",
                ArrivalAirport = $"{f.ArrivalAirport.AirportName} ({f.ArrivalAirport.AirportCode})",
                DepartureTime = f.DepartureTime,
                ArrivalTime = f.ArrivalTime,
                BasePrice = f.BasePrice,
                Status = f.Status,
                Gate = f.Gate,
                AvailableSeats = f.Seats.Count(s => s.IsAvailable == true && (string.IsNullOrEmpty(searchDto.SeatClass) || s.Class.ClassName == searchDto.SeatClass))
            }).ToList();
        }

        public async Task<SeatMapDto> GetSeatMapAsync(int flightId, int userId)
        {
            var flight = await _context.Flights
                .Include(f => f.AircraftType)
                .Include(f => f.Seats)
                    .ThenInclude(s => s.Class)
                .Include(f => f.Seats)
                    .ThenInclude(s => s.BookingSeats)
                        .ThenInclude(bs => bs.Booking)
                .FirstOrDefaultAsync(f => f.FlightId == flightId);

            if (flight == null)
                throw new ArgumentException("Flight not found");

            var seatMap = new SeatMapDto
            {
                FlightId = flight.FlightId,
                FlightNumber = flight.FlightNumber,
                AircraftModel = flight.AircraftType.AircraftModel,
                SeatMapLayout = flight.AircraftType.SeatMapLayout,
                Seats = flight.Seats.Select(s => new SeatDto
                {
                    SeatId = s.SeatId,
                    SeatNumber = s.SeatNumber,
                    SeatRow = s.SeatRow,
                    SeatColumn = s.SeatColumn,
                    SeatClassName = s.Class.ClassName,
                    IsWindow = s.IsWindow ?? false,
                    IsAisle = s.IsAisle ?? false,
                    IsEmergencyExit = s.IsEmergencyExit ?? false,
                    ExtraFee = s.ExtraFee ?? 0m,
                    IsAvailable = s.IsAvailable ?? false,
                    TotalPrice = flight.BasePrice * (s.Class.PriceMultiplier ?? 1.0m) + (s.ExtraFee ?? 0m),
                    IsBookedByCurrentUser = s.BookingSeats.Any(bs => bs.Booking.UserId == userId && bs.Booking.BookingStatus == "CONFIRMED")
                }).OrderBy(s => s.SeatRow).ThenBy(s => s.SeatColumn).ToList()
            };

            return seatMap;
        }

        public async Task<BookingResponseDto> CreateBookingAsync(CreateBookingDto bookingDto)
        {
            // Validate passenger details count matches passengers
            if (bookingDto.PassengerDetails.Count != bookingDto.Passengers)
            {
                throw new ArgumentException($"The number of passenger details ({bookingDto.PassengerDetails.Count}) does not match the number of passengers ({bookingDto.Passengers}).");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Validate flight and seat class
                var flight = await _context.Flights
                    .Include(f => f.Seats)
                        .ThenInclude(s => s.Class)
                    .FirstOrDefaultAsync(f => f.FlightId == bookingDto.FlightId);

                if (flight == null)
                    throw new ArgumentException("Flight not found");

                // Validate flight status - cannot book DELAYED, COMPLETED, CANCELLED, PREPARING, DEPARTED flights
                if (flight.Status == "DELAYED" || flight.Status == "COMPLETED" || 
                    flight.Status == "CANCELLED" || flight.Status == "PREPARING" || 
                    flight.Status == "DEPARTED")
                {
                    throw new InvalidOperationException($"Cannot book flight with status: {flight.Status}. Please select a scheduled flight.");
                }

                // Validate seat class
                var seatClass = await _context.SeatClasses
                    .FirstOrDefaultAsync(sc => sc.ClassId == bookingDto.SeatClassId);

                if (seatClass == null)
                    throw new ArgumentException("Seat class not found");

                // Get available seats for the selected class
                var availableSeats = flight.Seats
                    .Where(s => s.ClassId == bookingDto.SeatClassId && s.IsAvailable == true)
                    .ToList();

                if (availableSeats.Count < bookingDto.Passengers)
                    throw new InvalidOperationException($"Not enough available seats in the {seatClass.ClassName} class. Requested: {bookingDto.Passengers}, Available: {availableSeats.Count}");

                // Randomly select seats
                var random = new Random();
                var selectedSeats = availableSeats.OrderBy(x => random.Next()).Take(bookingDto.Passengers).ToList();

                // Calculate total amount
                var totalAmount = selectedSeats.Sum(s => flight.BasePrice * (s.Class.PriceMultiplier ?? 1.0m) + (s.ExtraFee ?? 0m));

                // Create booking
                var bookingReference = GenerateBookingReference();
                var booking = new Booking
                {
                    BookingReference = bookingReference,
                    UserId = bookingDto.UserId,
                    FlightId = bookingDto.FlightId,
                    TotalAmount = totalAmount,
                    Notes = bookingDto.Notes,
                    BookingStatus = "PENDING", // PENDING cho đến khi thanh toán thành công
                    PaymentStatus = "PENDING"
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // Create booking seats with individual passenger details
                for (int i = 0; i < selectedSeats.Count; i++)
                {
                    var seat = selectedSeats[i];
                    var passenger = bookingDto.PassengerDetails[i];
                    var bookingSeat = new BookingSeat
                    {
                        BookingId = booking.BookingId,
                        SeatId = seat.SeatId,
                        PassengerName = passenger.PassengerName,
                        PassengerIdNumber = passenger.PassengerIdNumber,
                        SeatPrice = flight.BasePrice * (seat.Class.PriceMultiplier ?? 1.0m) + (seat.ExtraFee ?? 0m)
                    };

                    _context.BookingSeats.Add(bookingSeat);

                    // Update seat availability
                    seat.IsAvailable = false;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // KHÔNG gửi email khi tạo booking - chỉ gửi khi thanh toán thành công
                // Email confirmation sẽ được gửi trong PaymentService khi payment success

                // Return booking details
                return await GetBookingByIdAsync(booking.BookingId);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private string GenerateBookingReference()
        {
            return $"VN{DateTime.Now:ddMMyyyy}{new Random().Next(1000, 9999)}";
        }

        public async Task<List<BookingResponseDto>> GetAllBookingsAsync()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Flight)
                    .ThenInclude(f => f.Airline)
                .Include(b => b.Flight.DepartureAirport)
                .Include(b => b.Flight.ArrivalAirport)
                .Include(b => b.BookingSeats)
                    .ThenInclude(bs => bs.Seat)
                        .ThenInclude(s => s.Class)
                .Include(b => b.User)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return bookings.Select(booking => new BookingResponseDto
            {
                BookingId = booking.BookingId,
                BookingReference = booking.BookingReference,
                BookingStatus = booking.BookingStatus,
                TotalAmount = booking.TotalAmount,
                PaymentStatus = booking.PaymentStatus,
                BookingDate = booking.BookingDate ?? DateTime.Now,
                Flight = new FlightResponseDto
                {
                    FlightId = booking.Flight.FlightId,
                    FlightNumber = booking.Flight.FlightNumber,
                    AirlineName = booking.Flight.Airline.AirlineName,
                    DepartureAirport = booking.Flight.DepartureAirport.AirportName,
                    ArrivalAirport = booking.Flight.ArrivalAirport.AirportName,
                    DepartureTime = booking.Flight.DepartureTime,
                    ArrivalTime = booking.Flight.ArrivalTime
                },
                Seats = booking.BookingSeats.Select(bs => new BookedSeatDto
                {
                    SeatNumber = bs.Seat.SeatNumber,
                    SeatClassName = bs.Seat.Class.ClassName,
                    PassengerName = bs.PassengerName,
                    SeatPrice = bs.SeatPrice
                }).ToList()
            }).ToList();
        }

        public async Task<BookingResponseDto> GetBookingByIdAsync(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Flight)
                    .ThenInclude(f => f.Airline)
                .Include(b => b.Flight.DepartureAirport)
                .Include(b => b.Flight.ArrivalAirport)
                .Include(b => b.BookingSeats)
                    .ThenInclude(bs => bs.Seat)
                        .ThenInclude(s => s.Class)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
                throw new ArgumentException("Booking not found");

            return new BookingResponseDto
            {
                BookingId = booking.BookingId,
                BookingReference = booking.BookingReference,
                BookingStatus = booking.BookingStatus,
                TotalAmount = booking.TotalAmount,
                PaymentStatus = booking.PaymentStatus,
                BookingDate = booking.BookingDate ?? DateTime.Now,
                Flight = new FlightResponseDto
                {
                    FlightId = booking.Flight.FlightId,
                    FlightNumber = booking.Flight.FlightNumber,
                    AirlineName = booking.Flight.Airline.AirlineName,
                    DepartureAirport = booking.Flight.DepartureAirport.AirportName,
                    ArrivalAirport = booking.Flight.ArrivalAirport.AirportName,
                    DepartureTime = booking.Flight.DepartureTime,
                    ArrivalTime = booking.Flight.ArrivalTime
                },
                Seats = booking.BookingSeats.Select(bs => new BookedSeatDto
                {
                    SeatNumber = bs.Seat.SeatNumber,
                    SeatClassName = bs.Seat.Class.ClassName,
                    PassengerName = bs.PassengerName,
                    SeatPrice = bs.SeatPrice
                }).ToList()
            };
        }

        public async Task<List<BookingResponseDto>> GetUserBookingsAsync(int userId)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Flight)
                    .ThenInclude(f => f.Airline)
                .Include(b => b.Flight.DepartureAirport)
                .Include(b => b.Flight.ArrivalAirport)
                .Include(b => b.BookingSeats)
                    .ThenInclude(bs => bs.Seat)
                        .ThenInclude(s => s.Class)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return bookings.Select(booking => new BookingResponseDto
            {
                BookingId = booking.BookingId,
                BookingReference = booking.BookingReference,
                BookingStatus = booking.BookingStatus,
                TotalAmount = booking.TotalAmount,
                PaymentStatus = booking.PaymentStatus,
                BookingDate = booking.BookingDate ?? DateTime.Now,
                Flight = new FlightResponseDto
                {
                    FlightId = booking.Flight.FlightId,
                    FlightNumber = booking.Flight.FlightNumber,
                    AirlineName = booking.Flight.Airline.AirlineName,
                    DepartureAirport = booking.Flight.DepartureAirport.AirportName,
                    ArrivalAirport = booking.Flight.ArrivalAirport.AirportName,
                    DepartureTime = booking.Flight.DepartureTime,
                    ArrivalTime = booking.Flight.ArrivalTime
                },
                Seats = booking.BookingSeats.Select(bs => new BookedSeatDto
                {
                    SeatNumber = bs.Seat.SeatNumber,
                    SeatClassName = bs.Seat.Class.ClassName,
                    PassengerName = bs.PassengerName,
                    SeatPrice = bs.SeatPrice
                }).ToList()
            }).ToList();
        }

        public async Task<bool> UpdateBookingStatusAsync(int bookingId, string status)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
                return false;

            booking.BookingStatus = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelBookingAsync(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingSeats)
                    .ThenInclude(bs => bs.Seat)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
                return false;

            // Free up seats
            foreach (var bookingSeat in booking.BookingSeats)
            {
                bookingSeat.Seat.IsAvailable = true;
            }

            booking.BookingStatus = "CANCELLED";
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ConfirmPaymentAsync(int paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null)
                throw new ArgumentException("Payment not found");

            // Update payment status
            payment.Status = "SUCCESS";
            await _context.SaveChangesAsync();

            // Sync booking payment status
            if (payment.Booking != null)
            {
                payment.Booking.PaymentStatus = "PAID";
                if (payment.Booking.BookingStatus != "CONFIRMED")
                {
                    payment.Booking.BookingStatus = "CONFIRMED";
                }
                await _context.SaveChangesAsync();
            }

            await _notificationService.SendPaymentConfirmationAsync(paymentId);
            return true;
        }

        public async Task<bool> RequestRestoreAsync(int bookingId, string? note = null)
        {
            var booking = await _context.Bookings
                .Include(b => b.Flight)
                .Include(b => b.BookingSeats)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) throw new ArgumentException("Booking not found");
            if (booking.BookingStatus != "CANCELLED") throw new InvalidOperationException("Only cancelled bookings can request restore");
            if (booking.Flight == null) throw new ArgumentException("Flight not found for booking");

            var now = DateTime.Now;
            if (booking.Flight.DepartureTime <= now.AddDays(2))
                throw new InvalidOperationException("Restore request must be at least 2 days before departure");

            booking.BookingStatus = "RESTORE_PENDING";
            booking.Notes = string.IsNullOrWhiteSpace(note) ? booking.Notes : note;
            await _context.SaveChangesAsync();

            // Notify user: request submitted
            await _notificationService.SendFlightUpdateAsync(booking.FlightId, "RESTORE_PENDING", $"Yêu cầu khôi phục vé {booking.BookingReference} đã được gửi. Vui lòng chờ duyệt.");
            return true;
        }
    }
}