using FlightBooking.DTOs;
using FlightBooking.DTOs.Admin;
using FlightBooking.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AircraftTypesController : ControllerBase
    {
        private readonly FlightBookingContext _context;

        public AircraftTypesController(FlightBookingContext context)
        {
            _context = context;
        }

/*        [HttpGet]
        public async Task<ActionResult<List<AircraftType>>> GetAllAircraftTypes()
        {
            try
            {
                var aircraftTypes = await _context.AircraftTypes
                    .OrderBy(at => at.Manufacturer)
                    .ThenBy(at => at.AircraftModel)
                    .ToListAsync();
                return Ok(aircraftTypes);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }*/

        [HttpGet]
        public async Task<ActionResult<List<AircraftTypeDto>>> GetAllAircraftTypes()
        {
            try
            {
                var aircraftTypes = await _context.AircraftTypes
                    .Include(at => at.Flights)
                        .ThenInclude(f => f.Airline)
                    .OrderBy(at => at.Manufacturer)
                    .ThenBy(at => at.AircraftModel)
                    .ToListAsync();

                var result = aircraftTypes.Select(at => new AircraftTypeDto
                {
                    AircraftTypeId = at.AircraftTypeId,
                    AircraftModel = at.AircraftModel,
                    Manufacturer = at.Manufacturer,
                    TotalSeats = at.TotalSeats,
                    EconomySeats = at.EconomySeats,
                    BusinessSeats = at.BusinessSeats,
                    FirstClassSeats = at.FirstClassSeats,
                    SeatMapLayout = at.SeatMapLayout,
                    Flights = at.Flights.Select(f => new FlightSummaryDto
                    {
                        FlightId = f.FlightId,
                        FlightNumber = f.FlightNumber,
                        AirlineName = f.Airline.AirlineName,
                        DepartureTime = f.DepartureTime,
                        ArrivalTime = f.ArrivalTime,
                        Status = f.Status
                    }).ToList()
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /*   [HttpPost]
           public async Task<ActionResult<AircraftType>> CreateAircraftType([FromBody] AircraftType aircraftType)
           {
               try
               {
                   _context.AircraftTypes.Add(aircraftType);
                   await _context.SaveChangesAsync();
                   return CreatedAtAction(nameof(GetAllAircraftTypes), aircraftType);
               }
               catch (Exception ex)
               {
                   return BadRequest(new { message = ex.Message });
               }
           }*/

        [HttpGet("{id}")]
        public async Task<ActionResult<AircraftTypeDto>> GetAircraftTypeById(int id)
        {
            try
            {
                var aircraftType = await _context.AircraftTypes
                    .Include(at => at.Flights)
                        .ThenInclude(f => f.Airline)
                    .FirstOrDefaultAsync(at => at.AircraftTypeId == id);

                if (aircraftType == null)
                    return NotFound(new { message = "Aircraft type not found" });

                var result = new AircraftTypeDto
                {
                    AircraftTypeId = aircraftType.AircraftTypeId,
                    AircraftModel = aircraftType.AircraftModel,
                    Manufacturer = aircraftType.Manufacturer,
                    TotalSeats = aircraftType.TotalSeats,
                    EconomySeats = aircraftType.EconomySeats,
                    BusinessSeats = aircraftType.BusinessSeats,
                    FirstClassSeats = aircraftType.FirstClassSeats,
                    SeatMapLayout = aircraftType.SeatMapLayout,
                    Flights = aircraftType.Flights.Select(f => new FlightSummaryDto
                    {
                        FlightId = f.FlightId,
                        FlightNumber = f.FlightNumber,
                        AirlineName = f.Airline.AirlineName,
                        DepartureTime = f.DepartureTime,
                        ArrivalTime = f.ArrivalTime,
                        Status = f.Status
                    }).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<AircraftTypeDto>> CreateAircraftType([FromBody] CreateAircraftTypeDto createDto)
        {
            try
            {
                // Validation logic
                if (createDto.EconomySeats + createDto.BusinessSeats + createDto.FirstClassSeats != createDto.TotalSeats)
                {
                    return BadRequest(new { message = "Total seats must equal sum of all seat classes" });
                }

                var aircraftType = new AircraftType
                {
                    AircraftModel = createDto.AircraftModel,
                    Manufacturer = createDto.Manufacturer,
                    TotalSeats = createDto.TotalSeats,
                    EconomySeats = createDto.EconomySeats,
                    BusinessSeats = createDto.BusinessSeats,
                    FirstClassSeats = createDto.FirstClassSeats,
                    SeatMapLayout = createDto.SeatMapLayout
                };

                _context.AircraftTypes.Add(aircraftType);
                await _context.SaveChangesAsync();

                // Return the created aircraft type with DTO format
                var result = new AircraftTypeDto
                {
                    AircraftTypeId = aircraftType.AircraftTypeId,
                    AircraftModel = aircraftType.AircraftModel,
                    Manufacturer = aircraftType.Manufacturer,
                    TotalSeats = aircraftType.TotalSeats,
                    EconomySeats = aircraftType.EconomySeats,
                    BusinessSeats = aircraftType.BusinessSeats,
                    FirstClassSeats = aircraftType.FirstClassSeats,
                    SeatMapLayout = aircraftType.SeatMapLayout,
                    Flights = new List<FlightSummaryDto>() // Empty list for new aircraft type
                };

                return CreatedAtAction(nameof(GetAircraftTypeById), new { id = aircraftType.AircraftTypeId }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<AircraftTypeDto>> UpdateAircraftType(int id, [FromBody] UpdateAircraftTypeDto updateDto)
        {
            try
            {
                var aircraftType = await _context.AircraftTypes.FindAsync(id);
                if (aircraftType == null)
                    return NotFound(new { message = "Aircraft type not found" });

                // Update only provided fields
                if (!string.IsNullOrEmpty(updateDto.AircraftModel))
                    aircraftType.AircraftModel = updateDto.AircraftModel;

                if (!string.IsNullOrEmpty(updateDto.Manufacturer))
                    aircraftType.Manufacturer = updateDto.Manufacturer;

                if (updateDto.TotalSeats.HasValue)
                    aircraftType.TotalSeats = updateDto.TotalSeats.Value;

                if (updateDto.EconomySeats.HasValue)
                    aircraftType.EconomySeats = updateDto.EconomySeats.Value;

                if (updateDto.BusinessSeats.HasValue)
                    aircraftType.BusinessSeats = updateDto.BusinessSeats.Value;

                if (updateDto.FirstClassSeats.HasValue)
                    aircraftType.FirstClassSeats = updateDto.FirstClassSeats.Value;

                if (!string.IsNullOrEmpty(updateDto.SeatMapLayout))
                    aircraftType.SeatMapLayout = updateDto.SeatMapLayout;

                // Validation after update
                if (aircraftType.EconomySeats + aircraftType.BusinessSeats + aircraftType.FirstClassSeats != aircraftType.TotalSeats)
                {
                    return BadRequest(new { message = "Total seats must equal sum of all seat classes" });
                }

                await _context.SaveChangesAsync();

                return await GetAircraftTypeById(id);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAircraftType(int id)
        {
            try
            {
                var aircraftType = await _context.AircraftTypes
                    .Include(at => at.Flights)
                    .FirstOrDefaultAsync(at => at.AircraftTypeId == id);

                if (aircraftType == null)
                    return NotFound(new { message = "Aircraft type not found" });

                // Check if aircraft type is being used by any flights
                if (aircraftType.Flights.Any())
                {
                    return BadRequest(new { message = "Cannot delete aircraft type that is being used by flights" });
                }

                _context.AircraftTypes.Remove(aircraftType);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
