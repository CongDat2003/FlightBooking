using FlightBooking.DTOs;

namespace FlightBooking.Services
{
    public interface IAIChatService
    {
        Task<AIChatResponseDto> SendMessageAsync(AIChatRequestDto request);
        Task<List<AIChatResponseDto>> GetChatHistoryAsync(int? userId, int limit = 20);
        Task ClearChatHistoryAsync(int? userId);
    }
}

