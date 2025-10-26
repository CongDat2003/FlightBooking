using FlightBooking.DTOs.Admin;
using FlightBooking.DTOs;
using FlightBooking.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AirportsController : ControllerBase
    {
        private readonly FlightBookingContext _context;

        public AirportsController(FlightBookingContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<AirportDto>>> GetAllAirports()
        {
            try
            {
                var airports = await _context.Airports
                    .Include(a => a.FlightDepartureAirports)
                        .ThenInclude(f => f.Airline)
                    .Include(a => a.FlightArrivalAirports)
                        .ThenInclude(f => f.Airline)
                    .Include(a => a.FlightDepartureAirports)
                        .ThenInclude(f => f.ArrivalAirport)
                    .Include(a => a.FlightArrivalAirports)
                        .ThenInclude(f => f.DepartureAirport)
                    .OrderBy(a => a.City)
                    .ThenBy(a => a.AirportName)
                    .ToListAsync();

                var result = airports.Select(a => new AirportDto
                {
                    AirportId = a.AirportId,
                    AirportCode = a.AirportCode,
                    AirportName = a.AirportName,
                    City = a.City,
                    Country = a.Country,
                    Timezone = a.Timezone,
                    CreatedAt = DateTime.Now,
                    TotalDepartureFlights = a.FlightDepartureAirports.Count,
                    TotalArrivalFlights = a.FlightArrivalAirports.Count,
                    DepartureFlights = a.FlightDepartureAirports.Select(f => new FlightSummaryDto
                    {
                        FlightId = f.FlightId,
                        FlightNumber = f.FlightNumber,
                        AirlineName = f.Airline.AirlineName,
                        DepartureTime = f.DepartureTime,
                        ArrivalTime = f.ArrivalTime,
                        BasePrice = f.BasePrice,
                        Status = f.Status,
                        ArrivalAirport = f.ArrivalAirport?.AirportCode
                    }).OrderBy(f => f.DepartureTime).ToList(),
                    ArrivalFlights = a.FlightArrivalAirports.Select(f => new FlightSummaryDto
                    {
                        FlightId = f.FlightId,
                        FlightNumber = f.FlightNumber,
                        AirlineName = f.Airline.AirlineName,
                        DepartureTime = f.DepartureTime,
                        ArrivalTime = f.ArrivalTime,
                        BasePrice = f.BasePrice,
                        Status = f.Status,
                        DepartureAirport = f.DepartureAirport?.AirportCode
                    }).OrderBy(f => f.ArrivalTime).ToList()
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{airportId}")]
        public async Task<ActionResult<AirportDto>> GetAirportById(int airportId)
        {
            try
            {
                var airport = await _context.Airports
                    .Include(a => a.FlightDepartureAirports)
                        .ThenInclude(f => f.Airline)
                    .Include(a => a.FlightArrivalAirports)
                        .ThenInclude(f => f.Airline)
                    .Include(a => a.FlightDepartureAirports)
                        .ThenInclude(f => f.ArrivalAirport)
                    .Include(a => a.FlightArrivalAirports)
                        .ThenInclude(f => f.DepartureAirport)
                    .FirstOrDefaultAsync(a => a.AirportId == airportId);

                if (airport == null)
                    return NotFound(new { message = "Airport not found" });

                var result = new AirportDto
                {
                    AirportId = airport.AirportId,
                    AirportCode = airport.AirportCode,
                    AirportName = airport.AirportName,
                    City = airport.City,
                    Country = airport.Country,
                    Timezone = airport.Timezone,
                    CreatedAt = DateTime.Now,
                    TotalDepartureFlights = airport.FlightDepartureAirports.Count,
                    TotalArrivalFlights = airport.FlightArrivalAirports.Count,
                    DepartureFlights = airport.FlightDepartureAirports.Select(f => new FlightSummaryDto
                    {
                        FlightId = f.FlightId,
                        FlightNumber = f.FlightNumber,
                        AirlineName = f.Airline.AirlineName,
                        DepartureTime = f.DepartureTime,
                        ArrivalTime = f.ArrivalTime,
                        BasePrice = f.BasePrice,
                        Status = f.Status,
                        ArrivalAirport = f.ArrivalAirport?.AirportCode
                    }).OrderBy(f => f.DepartureTime).ToList(),
                    ArrivalFlights = airport.FlightArrivalAirports.Select(f => new FlightSummaryDto
                    {
                        FlightId = f.FlightId,
                        FlightNumber = f.FlightNumber,
                        AirlineName = f.Airline.AirlineName,
                        DepartureTime = f.DepartureTime,
                        ArrivalTime = f.ArrivalTime,
                        BasePrice = f.BasePrice,
                        Status = f.Status,
                        DepartureAirport = f.DepartureAirport?.AirportCode
                    }).OrderBy(f => f.ArrivalTime).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<AirportDto>>> SearchAirports([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrEmpty(query))
                    return BadRequest(new { message = "Search query is required" });

                var airports = await _context.Airports
                    .Include(a => a.FlightDepartureAirports)
                    .Include(a => a.FlightArrivalAirports)
                    .Where(a => a.AirportCode.Contains(query) ||
                               a.AirportName.Contains(query) ||
                               a.City.Contains(query))
                    .OrderBy(a => a.City)
                    .ToListAsync();

                var result = airports.Select(a => new AirportDto
                {
                    AirportId = a.AirportId,
                    AirportCode = a.AirportCode,
                    AirportName = a.AirportName,
                    City = a.City,
                    Country = a.Country,
                    Timezone = a.Timezone,
                    CreatedAt = DateTime.Now,
                    TotalDepartureFlights = a.FlightDepartureAirports.Count,
                    TotalArrivalFlights = a.FlightArrivalAirports.Count,
                    DepartureFlights = new List<FlightSummaryDto>(), // Simplified for search
                    ArrivalFlights = new List<FlightSummaryDto>()
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("by-country/{country}")]
        public async Task<ActionResult<List<AirportDto>>> GetAirportsByCountry(string country)
        {
            try
            {
                var airports = await _context.Airports
                    .Include(a => a.FlightDepartureAirports)
                    .Include(a => a.FlightArrivalAirports)
                    .Where(a => a.Country.ToLower() == country.ToLower())
                    .OrderBy(a => a.City)
                    .ToListAsync();

                var result = airports.Select(a => new AirportDto
                {
                    AirportId = a.AirportId,
                    AirportCode = a.AirportCode,
                    AirportName = a.AirportName,
                    City = a.City,
                    Country = a.Country,
                    Timezone = a.Timezone,
                    CreatedAt = DateTime.Now,
                    TotalDepartureFlights = a.FlightDepartureAirports.Count,
                    TotalArrivalFlights = a.FlightArrivalAirports.Count,
                    DepartureFlights = new List<FlightSummaryDto>(), 
                    ArrivalFlights = new List<FlightSummaryDto>()
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<AirportDto>> CreateAirport([FromBody] CreateAirportDto createDto)
        {
            try
            {
                // Kiểm tra airport code đã tồn tại chưa
                var existingAirport = await _context.Airports
                    .FirstOrDefaultAsync(a => a.AirportCode == createDto.AirportCode);

                if (existingAirport != null)
                    return BadRequest(new { message = "Airport code already exists" });

                var airport = new Airport
                {
                    AirportCode = createDto.AirportCode,
                    AirportName = createDto.AirportName,
                    City = createDto.City,
                    Country = createDto.Country,
                    Timezone = createDto.Timezone,
                    CreatedAt = DateTime.Now
                };

                _context.Airports.Add(airport);
                await _context.SaveChangesAsync();

                var result = new AirportDto
                {
                    AirportId = airport.AirportId,
                    AirportCode = airport.AirportCode,
                    AirportName = airport.AirportName,
                    City = airport.City,
                    Country = airport.Country,
                    Timezone = airport.Timezone,
                    CreatedAt = DateTime.Now,
                    TotalDepartureFlights = 0,
                    TotalArrivalFlights = 0,
                    DepartureFlights = new List<FlightSummaryDto>(),
                    ArrivalFlights = new List<FlightSummaryDto>()
                };

                return CreatedAtAction(nameof(GetAirportById), new { airportId = airport.AirportId }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{airportId}")]
        public async Task<ActionResult<AirportDto>> UpdateAirport(int airportId, [FromBody] UpdateAirportDto updateDto)
        {
            try
            {
                var airport = await _context.Airports.FindAsync(airportId);
                if (airport == null)
                    return NotFound(new { message = "Airport not found" });

                // Kiểm tra airport code trùng (nếu có thay đổi)
                if (!string.IsNullOrEmpty(updateDto.AirportCode) && updateDto.AirportCode != airport.AirportCode)
                {
                    var existingAirport = await _context.Airports
                        .FirstOrDefaultAsync(a => a.AirportCode == updateDto.AirportCode && a.AirportId != airportId);

                    if (existingAirport != null)
                        return BadRequest(new { message = "Airport code already exists" });
                }

                // Update only provided fields
                if (!string.IsNullOrEmpty(updateDto.AirportCode))
                    airport.AirportCode = updateDto.AirportCode;

                if (!string.IsNullOrEmpty(updateDto.AirportName))
                    airport.AirportName = updateDto.AirportName;

                if (!string.IsNullOrEmpty(updateDto.City))
                    airport.City = updateDto.City;

                if (!string.IsNullOrEmpty(updateDto.Country))
                    airport.Country = updateDto.Country;

                if (updateDto.Timezone != null)
                    airport.Timezone = updateDto.Timezone;

                await _context.SaveChangesAsync();

                return await GetAirportById(airportId);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{airportId}")]
        public async Task<ActionResult> DeleteAirport(int airportId)
        {
            try
            {
                var airport = await _context.Airports
                    .Include(a => a.FlightDepartureAirports)
                    .Include(a => a.FlightArrivalAirports)
                    .FirstOrDefaultAsync(a => a.AirportId == airportId);

                if (airport == null)
                    return NotFound(new { message = "Airport not found" });

                // Kiểm tra có flights đang sử dụng airport này không
                if (airport.FlightDepartureAirports.Any() || airport.FlightArrivalAirports.Any())
                {
                    return BadRequest(new { message = "Cannot delete airport that has flights" });
                }

                _context.Airports.Remove(airport);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // API thống kê cho airport
        [HttpGet("{airportId}/stats")]
        public async Task<ActionResult> GetAirportStats(int airportId)
        {
            try
            {
                var airport = await _context.Airports
                    .Include(a => a.FlightDepartureAirports)
                    .Include(a => a.FlightArrivalAirports)
                    .FirstOrDefaultAsync(a => a.AirportId == airportId);

                if (airport == null)
                    return NotFound(new { message = "Airport not found" });

                var stats = new
                {
                    AirportId = airport.AirportId,
                    AirportName = airport.AirportName,
                    City = airport.City,
                    TotalDepartureFlights = airport.FlightDepartureAirports.Count,
                    TotalArrivalFlights = airport.FlightArrivalAirports.Count,
                    ScheduledDepartures = airport.FlightDepartureAirports.Count(f => f.Status == "SCHEDULED"),
                    ScheduledArrivals = airport.FlightArrivalAirports.Count(f => f.Status == "SCHEDULED"),
                    CompletedDepartures = airport.FlightDepartureAirports.Count(f => f.Status == "COMPLETED"),
                    CompletedArrivals = airport.FlightArrivalAirports.Count(f => f.Status == "COMPLETED"),
                    AverageDeparturePrice = airport.FlightDepartureAirports.Any() ? airport.FlightDepartureAirports.Average(f => f.BasePrice) : 0,
                    AverageArrivalPrice = airport.FlightArrivalAirports.Any() ? airport.FlightArrivalAirports.Average(f => f.BasePrice) : 0
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
