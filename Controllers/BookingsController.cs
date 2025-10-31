using FlightBooking.DTOs;
using FlightBooking.DTOs.Admin;
using FlightBooking.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace FlightBooking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly IFlightService _flightService;

        public BookingsController(IFlightService flightService)
        {
            _flightService = flightService;
        }

        [HttpPost]
        public async Task<ActionResult<BookingResponseDto>> CreateBooking([FromBody] CreateBookingDto bookingDto)
        {
            try
            {
                var booking = await _flightService.CreateBookingAsync(bookingDto);
                return CreatedAtAction(nameof(GetUserBookings), new { userId = bookingDto.UserId }, booking);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing your request.", detail = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult<List<BookingResponseDto>>> GetAllBookings()
        {
            try
            {
                var bookings = await _flightService.GetAllBookingsAsync();
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving bookings.", detail = ex.Message });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<BookingResponseDto>>> GetUserBookings(int userId)
        {
            try
            {
                var bookings = await _flightService.GetUserBookingsAsync(userId);
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving bookings.", detail = ex.Message });
            }
        }

        [HttpGet("{bookingId}")]
        public async Task<ActionResult<BookingResponseDto>> GetBookingById(int bookingId)
        {
            try
            {
                var booking = await _flightService.GetBookingByIdAsync(bookingId);
                if (booking == null)
                    return NotFound(new { message = "Booking not found" });
                
                return Ok(booking);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving booking.", detail = ex.Message });
            }
        }

        [HttpPut("{bookingId}/status")]
        public async Task<ActionResult> UpdateBookingStatus(int bookingId, [FromBody] UpdateBookingStatusDto statusDto)
        {
            try
            {
                var result = await _flightService.UpdateBookingStatusAsync(bookingId, statusDto.BookingStatus);
                if (!result)
                    return NotFound(new { message = "Booking not found" });
                
                return Ok(new { message = "Booking status updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating booking status.", detail = ex.Message });
            }
        }

        [HttpDelete("{bookingId}")]
        public async Task<ActionResult> CancelBooking(int bookingId)
        {
            try
            {
                var result = await _flightService.CancelBookingAsync(bookingId);
                if (!result)
                    return NotFound(new { message = "Booking not found" });
                
                return Ok(new { message = "Booking cancelled successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while cancelling booking.", detail = ex.Message });
            }
        }

        [HttpPost("{bookingId}/restore-request")]
        public async Task<ActionResult> RequestRestore(int bookingId, [FromBody] string? note = null)
        {
            try
            {
                var result = await _flightService.RequestRestoreAsync(bookingId, note);
                if (!result)
                    return NotFound(new { message = "Booking not found" });
                return Ok(new { message = "Restore request submitted" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while submitting restore request.", detail = ex.Message });
            }
        }
    }
}