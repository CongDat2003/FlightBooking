using FlightBooking.DTOs.Admin;
using FlightBooking.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlightBooking.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    public class FlightsController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public FlightsController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet]
        public async Task<ActionResult<List<AdminFlightResponseDto>>> GetAllFlights([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var flights = await _adminService.GetAllFlightsAsync(page, pageSize);
                return Ok(flights);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{flightId}")]
        public async Task<ActionResult<AdminFlightResponseDto>> GetFlightById(int flightId)
        {
            try
            {
                var flight = await _adminService.GetFlightByIdAsync(flightId);
                return Ok(flight);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<AdminFlightResponseDto>> CreateFlight([FromBody] CreateFlightDto flightDto)
        {
            try
            {
                var flight = await _adminService.CreateFlightAsync(flightDto);
                return CreatedAtAction(nameof(GetFlightById), new { flightId = flight.FlightId }, flight);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{flightId}")]
        public async Task<ActionResult<AdminFlightResponseDto>> UpdateFlight(int flightId, [FromBody] UpdateFlightDto flightDto)
        {
            try
            {
                var flight = await _adminService.UpdateFlightAsync(flightId, flightDto);
                return Ok(flight);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{flightId}")]
        public async Task<ActionResult> DeleteFlight(int flightId)
        {
            try
            {
                var result = await _adminService.DeleteFlightAsync(flightId);
                if (result)
                    return NoContent();
                return NotFound(new { message = "Flight not found" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{flightId}/generate-seats")]
        public async Task<ActionResult> GenerateSeats(int flightId)
        {
            try
            {
                var result = await _adminService.GenerateSeatsForFlightAsync(flightId);
                if (result)
                    return Ok(new { message = "Seats generated successfully" });
                return NotFound(new { message = "Flight not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
