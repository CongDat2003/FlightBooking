using FlightBooking.DTOs;

namespace FlightBooking.Services
{
    public interface IChatService
    {
        Task<MessageResponseDto> SendMessageAsync(CreateMessageDto messageDto);
        Task<ChatConversationDto> GetConversationAsync(int? userId = null);
        Task<MessageResponseDto> MarkAsReadAsync(int messageId);
        Task<List<MessageResponseDto>> GetUnreadMessagesAsync(int? userId = null);
        Task<MessageResponseDto> SendAdminReplyAsync(int messageId, string content);
        Task<List<AdminMessageResponseDto>> GetUnreadMessagesForAdminAsync();
        Task MarkMessagesAsReadAsync(int userId);
        Task<MessageResponseDto> SendAdminReplyByUserIdAsync(int userId, string content);
    }
}

