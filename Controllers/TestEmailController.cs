using FlightBooking.Models;
using FlightBooking.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestEmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly FlightBookingContext _context;

        public TestEmailController(IEmailService emailService, FlightBookingContext context)
        {
            _emailService = emailService;
            _context = context;
        }

        [HttpPost("test-booking-email/{bookingId}")]
        public async Task<ActionResult> TestBookingEmail(int bookingId)
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Flight)
                        .ThenInclude(f => f.Airline)
                    .Include(b => b.Flight.DepartureAirport)
                    .Include(b => b.Flight.ArrivalAirport)
                    .Include(b => b.BookingSeats)
                        .ThenInclude(bs => bs.Seat)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId);

                if (booking == null)
                    return NotFound("Booking not found");

                await _emailService.SendBookingConfirmationEmailAsync(booking);
                return Ok(new { message = "Test email sent successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
