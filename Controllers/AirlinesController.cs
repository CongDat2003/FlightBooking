using FlightBooking.DTOs;
using FlightBooking.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AirlinesController : ControllerBase
    {
        private readonly FlightBookingContext _context;

        public AirlinesController(FlightBookingContext context)
        {
            _context = context;
        }


        [HttpGet]
        public async Task<ActionResult<List<AirlineDto>>> GetAllAirlines()
        {
            try
            {
                var airlines = await _context.Airlines
                    .Include(a => a.Flights)
                        .ThenInclude(f => f.DepartureAirport)
                    .Include(a => a.Flights)
                        .ThenInclude(f => f.ArrivalAirport)
                    .OrderBy(a => a.AirlineName)
                    .ToListAsync();

                var result = airlines.Select(a => new AirlineDto
                {
                    AirlineId = a.AirlineId,
                    AirlineCode = a.AirlineCode,
                    AirlineName = a.AirlineName,
                    LogoUrl = a.LogoUrl,
                    CreatedAt = DateTime.Now,
                    Flights = a.Flights.Select(f => new FlightSummaryDto
                    {
                        FlightId = f.FlightId,
                        FlightNumber = f.FlightNumber,
                        DepartureTime = f.DepartureTime,
                        ArrivalTime = f.ArrivalTime,
                        BasePrice = f.BasePrice,
                        Status = f.Status,
                        // Thêm thông tin sân bay nếu cần
                        DepartureAirport = f.DepartureAirport?.AirportCode,
                        ArrivalAirport = f.ArrivalAirport?.AirportCode
                    }).OrderBy(f => f.DepartureTime).ToList()
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{airlineId}")]
        public async Task<ActionResult<AirlineDto>> GetAirlineById(int airlineId)
        {
            try
            {
                var airline = await _context.Airlines
                    .Include(a => a.Flights)
                        .ThenInclude(f => f.DepartureAirport)
                    .Include(a => a.Flights)
                        .ThenInclude(f => f.ArrivalAirport)
                    .FirstOrDefaultAsync(a => a.AirlineId == airlineId);

                if (airline == null)
                    return NotFound(new { message = "Airline not found" });

                var result = new AirlineDto
                {
                    AirlineId = airline.AirlineId,
                    AirlineCode = airline.AirlineCode,
                    AirlineName = airline.AirlineName,
                    LogoUrl = airline.LogoUrl,
                    CreatedAt = DateTime.Now,
                    Flights = airline.Flights.Select(f => new FlightSummaryDto
                    {
                        FlightId = f.FlightId,
                        FlightNumber = f.FlightNumber,
                        DepartureTime = f.DepartureTime,
                        ArrivalTime = f.ArrivalTime,
                        BasePrice = f.BasePrice,
                        Status = f.Status,
                        DepartureAirport = f.DepartureAirport?.AirportCode,
                        ArrivalAirport = f.ArrivalAirport?.AirportCode
                    }).OrderBy(f => f.DepartureTime).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<AirlineDto>> CreateAirline([FromBody] CreateAirlineDto createDto)
        {
            try
            {
                // Kiểm tra airline code đã tồn tại chưa
                var existingAirline = await _context.Airlines
                    .FirstOrDefaultAsync(a => a.AirlineCode == createDto.AirlineCode);

                if (existingAirline != null)
                    return BadRequest(new { message = "Airline code already exists" });

                var airline = new Airline
                {
                    AirlineCode = createDto.AirlineCode,
                    AirlineName = createDto.AirlineName,
                    LogoUrl = createDto.LogoUrl,
                    CreatedAt = DateTime.Now
                };

                _context.Airlines.Add(airline);
                await _context.SaveChangesAsync();

                var result = new AirlineDto
                {
                    AirlineId = airline.AirlineId,
                    AirlineCode = airline.AirlineCode,
                    AirlineName = airline.AirlineName,
                    LogoUrl = airline.LogoUrl,
                    CreatedAt = DateTime.Now,
                    Flights = new List<FlightSummaryDto>()
                };

                return CreatedAtAction(nameof(GetAirlineById), new { airlineId = airline.AirlineId }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{airlineId}")]
        public async Task<ActionResult<AirlineDto>> UpdateAirline(int airlineId, [FromBody] UpdateAirlineDto updateDto)
        {
            try
            {
                var airline = await _context.Airlines.FindAsync(airlineId);
                if (airline == null)
                    return NotFound(new { message = "Airline not found" });

                // Kiểm tra airline code trùng (nếu có thay đổi)
                if (!string.IsNullOrEmpty(updateDto.AirlineCode) && updateDto.AirlineCode != airline.AirlineCode)
                {
                    var existingAirline = await _context.Airlines
                        .FirstOrDefaultAsync(a => a.AirlineCode == updateDto.AirlineCode && a.AirlineId != airlineId);

                    if (existingAirline != null)
                        return BadRequest(new { message = "Airline code already exists" });
                }

                // Update only provided fields
                if (!string.IsNullOrEmpty(updateDto.AirlineCode))
                    airline.AirlineCode = updateDto.AirlineCode;

                if (!string.IsNullOrEmpty(updateDto.AirlineName))
                    airline.AirlineName = updateDto.AirlineName;

                if (updateDto.LogoUrl != null)
                    airline.LogoUrl = updateDto.LogoUrl;

                await _context.SaveChangesAsync();

                return await GetAirlineById(airlineId);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{airlineId}")]
        public async Task<ActionResult> DeleteAirline(int airlineId)
        {
            try
            {
                var airline = await _context.Airlines
                    .Include(a => a.Flights)
                    .FirstOrDefaultAsync(a => a.AirlineId == airlineId);

                if (airline == null)
                    return NotFound(new { message = "Airline not found" });

                // Kiểm tra có flights đang sử dụng airline này không
                if (airline.Flights.Any())
                {
                    return BadRequest(new { message = "Cannot delete airline that has flights" });
                }

                _context.Airlines.Remove(airline);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // API thống kê cho airline
        [HttpGet("{airlineId}/stats")]
        public async Task<ActionResult> GetAirlineStats(int airlineId)
        {
            try
            {
                var airline = await _context.Airlines
                    .Include(a => a.Flights)
                    .FirstOrDefaultAsync(a => a.AirlineId == airlineId);

                if (airline == null)
                    return NotFound(new { message = "Airline not found" });

                var stats = new
                {
                    AirlineId = airline.AirlineId,
                    AirlineName = airline.AirlineName,
                    TotalFlights = airline.Flights.Count,
                    ScheduledFlights = airline.Flights.Count(f => f.Status == "SCHEDULED"),
                    CompletedFlights = airline.Flights.Count(f => f.Status == "COMPLETED"),
                    CancelledFlights = airline.Flights.Count(f => f.Status == "CANCELLED"),
                    AveragePrice = airline.Flights.Any() ? airline.Flights.Average(f => f.BasePrice) : 0
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
