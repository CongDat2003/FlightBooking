using FlightBooking.DTOs;
using FlightBooking.Models;
using FlightBooking.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    public class SeatsController : ControllerBase
    {
        private readonly FlightBookingContext _context;
        private readonly INotificationService _notificationService;

        public SeatsController(FlightBookingContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        [HttpGet("by-flight/{flightId}")]
        public async Task<ActionResult<SeatMapDto>> GetSeatsByFlight(int flightId)
        {
            var flight = await _context.Flights
                .Include(f => f.AircraftType)
                .Include(f => f.Seats).ThenInclude(s => s.Class)
                .FirstOrDefaultAsync(f => f.FlightId == flightId);

            if (flight == null) return NotFound(new { message = "Flight not found" });

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
                    IsBookedByCurrentUser = false
                }).OrderBy(s => s.SeatRow).ThenBy(s => s.SeatColumn).ToList()
            };

            return Ok(seatMap);
        }

        public class UpdateSeatDto
        {
            public bool? IsAvailable { get; set; }
            public decimal? ExtraFee { get; set; }
            public int? ClassId { get; set; }
            public bool? IsEmergencyExit { get; set; }
        }

        [HttpPut("{seatId}")]
        public async Task<ActionResult> UpdateSeat(int seatId, [FromBody] UpdateSeatDto dto)
        {
            var seat = await _context.Seats
                .Include(s => s.BookingSeats)
                .Include(s => s.Flight)
                .FirstOrDefaultAsync(s => s.SeatId == seatId);
            if (seat == null) return NotFound(new { message = "Seat not found" });

            if (seat.BookingSeats.Any())
            {
                if (dto.ClassId.HasValue || (dto.IsAvailable.HasValue && dto.IsAvailable.Value == false))
                {
                    return BadRequest(new { message = "Cannot modify class or set unavailable for booked seat" });
                }
            }

            string? notificationMessage = null;
            bool wasAvailable = seat.IsAvailable ?? true;
            decimal? oldExtraFee = seat.ExtraFee;

            if (dto.IsAvailable.HasValue) 
            {
                seat.IsAvailable = dto.IsAvailable.Value;
                // Thông báo khi khóa/mở ghế
                if (dto.IsAvailable.Value && !wasAvailable)
                {
                    notificationMessage = $"Ghế {seat.SeatNumber} của chuyến bay {seat.Flight.FlightNumber} đã được mở lại và có thể đặt.";
                }
                else if (!dto.IsAvailable.Value && wasAvailable)
                {
                    notificationMessage = $"Ghế {seat.SeatNumber} của chuyến bay {seat.Flight.FlightNumber} đã bị khóa và không thể đặt.";
                }
            }
            
            if (dto.ExtraFee.HasValue) 
            {
                seat.ExtraFee = dto.ExtraFee.Value;
                // Thông báo khi đặt lại phụ ghế
                if (dto.ExtraFee.Value != (oldExtraFee ?? 0))
                {
                    notificationMessage = $"Phụ phí ghế {seat.SeatNumber} của chuyến bay {seat.Flight.FlightNumber} đã được cập nhật từ {oldExtraFee ?? 0:N0} VND thành {dto.ExtraFee.Value:N0} VND.";
                }
            }
            
            if (dto.ClassId.HasValue) seat.ClassId = dto.ClassId.Value;
            if (dto.IsEmergencyExit.HasValue) seat.IsEmergencyExit = dto.IsEmergencyExit.Value;

            await _context.SaveChangesAsync();

            // Gửi thông báo nếu có thay đổi
            if (!string.IsNullOrEmpty(notificationMessage) && seat.Flight != null)
            {
                await _notificationService.SendFlightUpdateAsync(seat.Flight.FlightId, "SEAT_UPDATE", notificationMessage);
            }

            return NoContent();
        }

        public class BulkUpdateSeatDto
        {
            public List<int> SeatIds { get; set; } = new();
            public bool? IsAvailable { get; set; }
            public decimal? ExtraFee { get; set; }
            public int? ClassId { get; set; }
        }

        [HttpPut("bulk")]
        public async Task<ActionResult> BulkUpdate([FromBody] BulkUpdateSeatDto dto)
        {
            if (dto.SeatIds == null || dto.SeatIds.Count == 0)
            {
                return BadRequest(new { message = "SeatIds is required" });
            }

            var seats = await _context.Seats
                .Include(s => s.BookingSeats)
                .Include(s => s.Flight)
                .Where(s => dto.SeatIds.Contains(s.SeatId))
                .ToListAsync();

            if (!seats.Any())
            {
                return NotFound(new { message = "No seats found" });
            }

            var flightIds = seats.Select(s => s.FlightId).Distinct().ToList();
            var flights = await _context.Flights
                .Where(f => flightIds.Contains(f.FlightId))
                .ToListAsync();

            int updatedCount = 0;
            string? notificationMessage = null;

            foreach (var seat in seats)
            {
                if (seat.BookingSeats.Any())
                {
                    if (dto.ClassId.HasValue || (dto.IsAvailable.HasValue && dto.IsAvailable.Value == false))
                        continue;
                }

                bool wasAvailable = seat.IsAvailable ?? true;
                decimal? oldExtraFee = seat.ExtraFee;

                if (dto.IsAvailable.HasValue) 
                {
                    seat.IsAvailable = dto.IsAvailable.Value;
                    if (dto.IsAvailable.Value != wasAvailable)
                        updatedCount++;
                }
                
                if (dto.ExtraFee.HasValue) 
                {
                    seat.ExtraFee = dto.ExtraFee.Value;
                    if (dto.ExtraFee.Value != (oldExtraFee ?? 0))
                        updatedCount++;
                }
                
                if (dto.ClassId.HasValue) 
                {
                    seat.ClassId = dto.ClassId.Value;
                    updatedCount++;
                }
            }

            await _context.SaveChangesAsync();

            // Gửi thông báo cho từng flight
            foreach (var flightId in flightIds)
            {
                var flightSeats = seats.Where(s => s.FlightId == flightId).ToList();
                var flight = flights.FirstOrDefault(f => f.FlightId == flightId);
                
                if (flight != null && flightSeats.Any())
                {
                    if (dto.IsAvailable.HasValue)
                    {
                        if (dto.IsAvailable.Value)
                        {
                            notificationMessage = $"{flightSeats.Count} ghế của chuyến bay {flight.FlightNumber} đã được mở lại và có thể đặt.";
                        }
                        else
                        {
                            notificationMessage = $"{flightSeats.Count} ghế của chuyến bay {flight.FlightNumber} đã bị khóa và không thể đặt.";
                        }
                    }
                    else if (dto.ExtraFee.HasValue)
                    {
                        notificationMessage = $"Phụ phí của {flightSeats.Count} ghế trong chuyến bay {flight.FlightNumber} đã được cập nhật thành {dto.ExtraFee.Value:N0} VND.";
                    }
                    else if (dto.ClassId.HasValue)
                    {
                        notificationMessage = $"Hạng ghế của {flightSeats.Count} ghế trong chuyến bay {flight.FlightNumber} đã được cập nhật.";
                    }

                    if (!string.IsNullOrEmpty(notificationMessage))
                    {
                        await _notificationService.SendFlightUpdateAsync(flightId, "SEAT_UPDATE", notificationMessage);
                    }
                }
            }

            return NoContent();
        }
    }
}




































































