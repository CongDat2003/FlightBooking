using FlightBooking.DTOs;
using FlightBooking.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpGet]
        public async Task<ActionResult<List<PaymentResponseDto>>> GetAllPayments()
        {
            try
            {
                var payments = await _paymentService.GetAllPaymentsAsync();
                return Ok(payments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving payments.", detail = ex.Message });
            }
        }

        [HttpGet("{paymentId}")]
        public async Task<ActionResult<PaymentResponseDto>> GetPaymentById(int paymentId)
        {
            try
            {
                var payment = await _paymentService.GetPaymentByIdAsync(paymentId);
                return Ok(payment);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving payment.", detail = ex.Message });
            }
        }

        [HttpPost("create")]
        public async Task<ActionResult<PaymentResponseDto>> CreatePayment([FromBody] CreatePaymentDto paymentDto)
        {
            try
            {
                var payment = await _paymentService.CreatePaymentAsync(paymentDto);
                return Ok(payment);
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

        [HttpPost("callback")]
        public async Task<ActionResult<PaymentResponseDto>> PaymentCallback([FromBody] PaymentCallbackDto callbackDto)
        {
            try
            {
                var payment = await _paymentService.ProcessCallbackAsync(callbackDto);
                return Ok(payment);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("status/{transactionId}")]
        public async Task<ActionResult<PaymentResponseDto>> GetPaymentStatus(string transactionId)
        {
            try
            {
                var payment = await _paymentService.GetPaymentStatusAsync(transactionId);
                return Ok(payment);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{paymentId}/refund")]
        public async Task<ActionResult> RefundPayment(int paymentId, [FromBody] decimal? refundAmount = null)
        {
            try
            {
                var result = await _paymentService.RefundPaymentAsync(paymentId, refundAmount);
                if (result)
                    return Ok(new { message = "Refund processed successfully" });
                return BadRequest(new { message = "Refund failed" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
