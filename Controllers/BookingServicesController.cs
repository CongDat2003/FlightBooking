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
    public class BookingServicesController : ControllerBase
    {
        private readonly IServiceService _serviceService;

        public BookingServicesController(IServiceService serviceService)
        {
            _serviceService = serviceService;
        }

        [HttpGet("booking/{bookingId}")]
        public async Task<ActionResult<List<BookingServiceDto>>> GetBookingServices(int bookingId)
        {
            try
            {
                var services = await _serviceService.GetBookingServicesAsync(bookingId);
                return Ok(services);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving booking services.", detail = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<BookingServiceDto>> AddServiceToBooking([FromBody] AddServiceToBookingDto addDto)
        {
            try
            {
                var bookingService = await _serviceService.AddServiceToBookingAsync(addDto);
                return CreatedAtAction(nameof(GetBookingServices), new { bookingId = addDto.BookingId }, bookingService);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while adding service to booking.", detail = ex.Message });
            }
        }

        [HttpDelete("{bookingServiceId}")]
        public async Task<ActionResult> RemoveServiceFromBooking(int bookingServiceId)
        {
            try
            {
                var result = await _serviceService.RemoveServiceFromBookingAsync(bookingServiceId);
                if (!result)
                    return NotFound(new { message = "Booking service not found" });
                
                return Ok(new { message = "Service removed from booking successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while removing service from booking.", detail = ex.Message });
            }
        }

        [HttpGet("booking/{bookingId}/total")]
        public async Task<ActionResult<decimal>> GetBookingServicesTotal(int bookingId)
        {
            try
            {
                var total = await _serviceService.CalculateBookingServicesTotalAsync(bookingId);
                return Ok(new { total });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while calculating total.", detail = ex.Message });
            }
        }
    }
}







































































