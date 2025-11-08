using FlightBooking.DTOs;
using FlightBooking.DTOs.Admin;
using FlightBooking.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlightBooking.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/Bookings")]
    public class AdminBookingsController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminBookingsController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpPost]
        public async Task<ActionResult<AdminBookingResponseDto>> CreateBooking([FromBody] CreateBookingDto bookingDto)
        {
            try
            {
                var booking = await _adminService.CreateBookingAsync(bookingDto);
                return CreatedAtAction(nameof(GetBookingById), new { bookingId = booking.BookingId }, booking);
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
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult<List<AdminBookingResponseDto>>> GetAllBookings([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var bookings = await _adminService.GetAllBookingsAsync(page, pageSize);
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{bookingId}")]
        public async Task<ActionResult<AdminBookingResponseDto>> GetBookingById(int bookingId)
        {
            try
            {
                var booking = await _adminService.GetBookingByIdAsync(bookingId);
                return Ok(booking);
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

        [HttpPut("{bookingId}/status")]
        public async Task<ActionResult<AdminBookingResponseDto>> UpdateBookingStatus(int bookingId, [FromBody] UpdateBookingStatusDto statusDto)
        {
            try
            {
                var booking = await _adminService.UpdateBookingStatusAsync(bookingId, statusDto);
                return Ok(booking);
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

        [HttpPost("{bookingId}/cancel")]
        public async Task<ActionResult> CancelBooking(int bookingId)
        {
            try
            {
                var result = await _adminService.CancelBookingAsync(bookingId);
                if (result)
                    return Ok(new { message = "Booking cancelled successfully" });
                return NotFound(new { message = "Booking not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{bookingId}")]
        public async Task<ActionResult> DeleteBooking(int bookingId)
        {
            try
            {
                var result = await _adminService.DeleteBookingPermanentlyAsync(bookingId);
                if (result)
                    return NoContent();
                return NotFound(new { message = "Booking not found" });
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

        [HttpPost("{bookingId}/restore-approve")]
        public async Task<ActionResult> ApproveRestore(int bookingId, [FromBody] string? note = null)
        {
            try
            {
                var result = await _adminService.ApproveRestoreAsync(bookingId, note);
                if (result)
                    return Ok(new { message = "Restore approved" });
                return NotFound(new { message = "Booking not found" });
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

        [HttpPost("{bookingId}/restore-reject")]
        public async Task<ActionResult> RejectRestore(int bookingId, [FromBody] string? note = null)
        {
            try
            {
                var result = await _adminService.RejectRestoreAsync(bookingId, note);
                if (result)
                    return Ok(new { message = "Restore rejected" });
                return NotFound(new { message = "Booking not found" });
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
    }
}
