using FlightBooking.DTOs;
using FlightBooking.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlightBooking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        [HttpPost("send")]
        public async Task<ActionResult<MessageResponseDto>> SendMessage([FromBody] CreateMessageDto messageDto)
        {
            _logger.LogInformation($"=== ChatController.SendMessage ===");
            _logger.LogInformation($"Received: UserId={messageDto.UserId}, Content length={messageDto.Content?.Length ?? 0}, SenderType={messageDto.SenderType}");
            
            try
            {
                if (messageDto == null)
                {
                    _logger.LogError("MessageDto is null");
                    return BadRequest(new { message = "Request body is required" });
                }
                
                var message = await _chatService.SendMessageAsync(messageDto);
                _logger.LogInformation($"Message sent successfully: MessageId={message.MessageId}");
                return Ok(message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Validation error: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("conversation")]
        public async Task<ActionResult<ChatConversationDto>> GetConversation([FromQuery] int? userId = null)
        {
            _logger.LogInformation($"=== ChatController.GetConversation - userId: {userId} ===");
            
            try
            {
                var conversation = await _chatService.GetConversationAsync(userId);
                _logger.LogInformation($"Conversation loaded: {conversation.Messages.Count} messages, {conversation.UnreadCount} unread");
                return Ok(conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting conversation for userId: {userId}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{messageId}/read")]
        public async Task<ActionResult<MessageResponseDto>> MarkAsRead(int messageId)
        {
            try
            {
                var message = await _chatService.MarkAsReadAsync(messageId);
                return Ok(message);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message as read");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("unread")]
        public async Task<ActionResult<List<MessageResponseDto>>> GetUnreadMessages([FromQuery] int? userId = null)
        {
            try
            {
                var messages = await _chatService.GetUnreadMessagesAsync(userId);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread messages");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("admin/unread")]
        public async Task<ActionResult<List<AdminMessageResponseDto>>> GetUnreadMessagesForAdmin()
        {
            try
            {
                var messages = await _chatService.GetUnreadMessagesForAdminAsync();
                return Ok(messages ?? new List<AdminMessageResponseDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread messages for admin: {Error}", ex.Message);
                // Return empty list instead of BadRequest for better UX
                return Ok(new List<AdminMessageResponseDto>());
            }
        }


        [HttpPost("mark-read/{userId}")]
        public async Task<IActionResult> MarkMessagesAsRead(int userId)
        {
            try
            {
                await _chatService.MarkMessagesAsReadAsync(userId);
                return Ok(new { message = "Messages marked as read." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking messages as read for user {userId}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{messageId}/reply")]
        public async Task<ActionResult<MessageResponseDto>> SendAdminReply(int messageId, [FromBody] CreateMessageDto replyDto)
        {
            try
            {
                var reply = await _chatService.SendAdminReplyAsync(messageId, replyDto.Content);
                return Ok(reply);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending admin reply");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("admin/reply")]
        public async Task<ActionResult<MessageResponseDto>> SendAdminReply([FromBody] AdminReplyDto adminReplyDto)
        {
            try
            {
                var message = await _chatService.SendAdminReplyByUserIdAsync(adminReplyDto.UserId, adminReplyDto.Content);
                return Ok(message);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending admin reply");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

