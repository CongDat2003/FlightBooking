using FlightBooking.DTOs;
using FlightBooking.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlightBooking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FlightsController : ControllerBase
    {
        private readonly IFlightService _flightService;

        public FlightsController(IFlightService flightService)
        {
            _flightService = flightService;
        }

        [HttpPost("search")]
        public async Task<ActionResult<List<FlightResponseDto>>> SearchFlights([FromBody] FlightSearchDto searchDto)
        {
            try
            {
                var flights = await _flightService.SearchFlightsAsync(searchDto);
                return Ok(flights);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{flightId}/seats")]
        public async Task<ActionResult<SeatMapDto>> GetSeatMap(int flightId, [FromQuery] int userId)
        {
            try
            {
                var seatMap = await _flightService.GetSeatMapAsync(flightId, userId);
                return Ok(seatMap);
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
    }
}
