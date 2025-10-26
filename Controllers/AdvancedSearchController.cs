using FlightBooking.DTOs;
using FlightBooking.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlightBooking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdvancedSearchController : ControllerBase
    {
        private readonly IAdvancedSearchService _searchService;

        public AdvancedSearchController(IAdvancedSearchService searchService)
        {
            _searchService = searchService;
        }

        [HttpPost("flights")]
        public async Task<ActionResult<FlightSearchResultDto>> AdvancedSearch([FromBody] AdvancedFlightSearchDto searchDto)
        {
            try
            {
                var result = await _searchService.AdvancedSearchAsync(searchDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("popular-destinations/{fromAirport}")]
        public async Task<ActionResult<List<string>>> GetPopularDestinations(string fromAirport)
        {
            try
            {
                var destinations = await _searchService.GetPopularDestinationsAsync(fromAirport);
                return Ok(destinations);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("recommendations/{userId}")]
        public async Task<ActionResult<List<FlightResponseDto>>> GetRecommendations(int userId)
        {
            try
            {
                var recommendations = await _searchService.GetRecommendedFlightsAsync(userId);
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("price-trend/{route}")]
        public async Task<ActionResult<PriceTrendDto>> GetPriceTrend(string route, [FromQuery] int days = 30)
        {
            try
            {
                var trend = await _searchService.GetPriceTrendAsync(route, days);
                return Ok(trend);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}