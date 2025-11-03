using FlightBooking.DTOs;
using FlightBooking.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    public class SeatsController : ControllerBase
    {
        private readonly FlightBookingContext _context;

        public SeatsController(FlightBookingContext context)
        {
            _context = context;
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
                .FirstOrDefaultAsync(s => s.SeatId == seatId);
            if (seat == null) return NotFound(new { message = "Seat not found" });

            if (seat.BookingSeats.Any())
            {
                if (dto.ClassId.HasValue || (dto.IsAvailable.HasValue && dto.IsAvailable.Value == false))
                {
                    return BadRequest(new { message = "Cannot modify class or set unavailable for booked seat" });
                }
            }

            if (dto.IsAvailable.HasValue) seat.IsAvailable = dto.IsAvailable.Value;
            if (dto.ExtraFee.HasValue) seat.ExtraFee = dto.ExtraFee.Value;
            if (dto.ClassId.HasValue) seat.ClassId = dto.ClassId.Value;
            if (dto.IsEmergencyExit.HasValue) seat.IsEmergencyExit = dto.IsEmergencyExit.Value;

            await _context.SaveChangesAsync();
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
                .Where(s => dto.SeatIds.Contains(s.SeatId))
                .ToListAsync();

            foreach (var seat in seats)
            {
                if (seat.BookingSeats.Any())
                {
                    if (dto.ClassId.HasValue || (dto.IsAvailable.HasValue && dto.IsAvailable.Value == false))
                        continue;
                }

                if (dto.IsAvailable.HasValue) seat.IsAvailable = dto.IsAvailable.Value;
                if (dto.ExtraFee.HasValue) seat.ExtraFee = dto.ExtraFee.Value;
                if (dto.ClassId.HasValue) seat.ClassId = dto.ClassId.Value;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}





