using FlightBooking.DTOs;
using FlightBooking.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightBooking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LuggagesController : ControllerBase
    {
        private readonly IServiceService _serviceService;

        public LuggagesController(IServiceService serviceService)
        {
            _serviceService = serviceService;
        }

        [HttpGet]
        public async Task<ActionResult<List<LuggageDto>>> GetAllLuggages()
        {
            try
            {
                var luggages = await _serviceService.GetAllLuggagesAsync();
                return Ok(luggages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving luggages.", detail = ex.Message });
            }
        }

        [HttpGet("{luggageId}")]
        public async Task<ActionResult<LuggageDto>> GetLuggageById(int luggageId)
        {
            try
            {
                var luggage = await _serviceService.GetLuggageByIdAsync(luggageId);
                if (luggage == null)
                    return NotFound(new { message = "Luggage not found" });
                
                return Ok(luggage);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving luggage.", detail = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<LuggageDto>> CreateLuggage([FromBody] CreateLuggageDto createDto)
        {
            try
            {
                var luggage = await _serviceService.CreateLuggageAsync(createDto);
                return CreatedAtAction(nameof(GetLuggageById), new { luggageId = luggage.LuggageId }, luggage);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating luggage.", detail = ex.Message });
            }
        }

        [HttpPut("{luggageId}")]
        public async Task<ActionResult> UpdateLuggage(int luggageId, [FromBody] UpdateLuggageDto updateDto)
        {
            try
            {
                var result = await _serviceService.UpdateLuggageAsync(luggageId, updateDto);
                if (!result)
                    return NotFound(new { message = "Luggage not found" });
                
                return Ok(new { message = "Luggage updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating luggage.", detail = ex.Message });
            }
        }

        [HttpDelete("{luggageId}")]
        public async Task<ActionResult> DeleteLuggage(int luggageId)
        {
            try
            {
                var result = await _serviceService.DeleteLuggageAsync(luggageId);
                if (!result)
                    return NotFound(new { message = "Luggage not found" });
                
                return Ok(new { message = "Luggage deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting luggage.", detail = ex.Message });
            }
        }
    }
}


