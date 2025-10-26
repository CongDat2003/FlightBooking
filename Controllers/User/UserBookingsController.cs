using FlightBooking.DTOs.User;
using FlightBooking.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlightBooking.Controllers.User
{
    [ApiController]
    [Route("api/user/[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly IUserService _userService;

        public BookingsController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("{userId}/history")]
        public async Task<ActionResult<List<UserBookingHistoryDto>>> GetBookingHistory(int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var bookings = await _userService.GetBookingHistoryAsync(userId, page, pageSize);
                return Ok(bookings);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{userId}/{bookingId}")]
        public async Task<ActionResult<BookingDetailDto>> GetBookingDetail(int userId, int bookingId)
        {
            try
            {
                var booking = await _userService.GetBookingDetailAsync(userId, bookingId);
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

        [HttpPost("{userId}/{bookingId}/cancel")]
        public async Task<ActionResult> CancelBooking(int userId, int bookingId)
        {
            try
            {
                var result = await _userService.CancelBookingAsync(userId, bookingId);
                if (result)
                    return Ok(new { message = "Booking cancelled successfully" });
                return BadRequest(new { message = "Failed to cancel booking" });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
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
