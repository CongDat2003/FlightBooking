using FlightBooking.DTOs;
using FlightBooking.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlightBooking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AIChatController : ControllerBase
    {
        private readonly IAIChatService _aiChatService;
        private readonly ILogger<AIChatController> _logger;

        public AIChatController(IAIChatService aiChatService, ILogger<AIChatController> logger)
        {
            _aiChatService = aiChatService;
            _logger = logger;
        }

        [HttpPost("send")]
        public async Task<ActionResult<AIChatResponseDto>> SendMessage([FromBody] AIChatRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.UserMessage))
                {
                    return BadRequest(new { message = "Nội dung tin nhắn không được để trống" });
                }

                var response = await _aiChatService.SendMessageAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending AI chat message: {Message}", ex.Message);
                _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
                return BadRequest(new { message = $"Đã có lỗi xảy ra khi xử lý tin nhắn: {ex.Message}" });
            }
        }

        [HttpGet("history")]
        public async Task<ActionResult<List<AIChatResponseDto>>> GetChatHistory([FromQuery] int? userId = null, [FromQuery] int limit = 20)
        {
            try
            {
                var history = await _aiChatService.GetChatHistoryAsync(userId, limit);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI chat history");
                return BadRequest(new { message = "Đã có lỗi xảy ra khi lấy lịch sử chat" });
            }
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearChatHistory([FromQuery] int? userId = null)
        {
            try
            {
                if (!userId.HasValue)
                {
                    return BadRequest(new { message = "UserId là bắt buộc" });
                }

                await _aiChatService.ClearChatHistoryAsync(userId.Value);
                return Ok(new { message = "Đã xóa lịch sử chat thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing AI chat history");
                return BadRequest(new { message = "Đã có lỗi xảy ra khi xóa lịch sử chat" });
            }
        }
    }
}





