using FlightBooking.DTOs;
using FlightBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Services
{
    public class AIChatService : IAIChatService
    {
        private readonly FlightBookingContext _context;
        private readonly IGeminiAIService _geminiService;
        private readonly ILogger<AIChatService> _logger;

        public AIChatService(
            FlightBookingContext context,
            IGeminiAIService geminiService,
            ILogger<AIChatService> logger)
        {
            _context = context;
            _geminiService = geminiService;
            _logger = logger;
        }

        public async Task<AIChatResponseDto> SendMessageAsync(AIChatRequestDto request)
        {
            try
            {
                // Lấy lịch sử chat gần đây để context (chỉ khi userId hợp lệ)
                var chatHistory = new List<(string UserMessage, string AIResponse)>();
                
                if (request.UserId.HasValue && request.UserId.Value > 0)
                {
                    var recentHistory = await _context.AIChatMessages
                        .Where(m => m.UserId == request.UserId.Value)
                        .OrderByDescending(m => m.CreatedAt)
                        .Take(5)
                        .OrderBy(m => m.CreatedAt)
                        .Select(m => new { m.UserMessage, m.AIResponse })
                        .ToListAsync();

                    chatHistory = recentHistory.Select(m => (m.UserMessage, m.AIResponse)).ToList();
                }

                // Gọi Gemini AI để lấy phản hồi (truyền isAdmin để phân biệt)
                var aiResponse = await _geminiService.GetAIResponseAsync(
                    request.UserMessage,
                    request.UserId,
                    chatHistory,
                    request.IsAdmin);

                // Lưu vào database
                var aiChatMessage = new AIChatMessage
                {
                    UserId = request.UserId > 0 ? request.UserId : null,
                    UserMessage = request.UserMessage,
                    AIResponse = aiResponse,
                    SessionId = request.SessionId ?? Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.Now
                };

                _context.AIChatMessages.Add(aiChatMessage);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"AI Chat message saved: MessageId={aiChatMessage.MessageId}");

                return new AIChatResponseDto
                {
                    MessageId = aiChatMessage.MessageId,
                    UserMessage = aiChatMessage.UserMessage,
                    AIResponse = aiChatMessage.AIResponse,
                    CreatedAt = aiChatMessage.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending AI chat message: {Message}", ex.Message);
                _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
                throw;
            }
        }

        public async Task<List<AIChatResponseDto>> GetChatHistoryAsync(int? userId, int limit = 20)
        {
            try
            {
                var query = _context.AIChatMessages
                    .OrderByDescending(m => m.CreatedAt)
                    .AsQueryable();

                if (userId.HasValue)
                {
                    query = query.Where(m => m.UserId == userId.Value);
                }

                var messages = await query
                    .Take(limit)
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync();

                return messages.Select(m => new AIChatResponseDto
                {
                    MessageId = m.MessageId,
                    UserMessage = m.UserMessage,
                    AIResponse = m.AIResponse,
                    CreatedAt = m.CreatedAt
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI chat history");
                throw;
            }
        }

        public async Task ClearChatHistoryAsync(int? userId)
        {
            try
            {
                var messages = await _context.AIChatMessages
                    .Where(m => m.UserId == userId)
                    .ToListAsync();

                _context.AIChatMessages.RemoveRange(messages);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Cleared AI chat history for userId: {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing AI chat history");
                throw;
            }
        }
    }
}



























