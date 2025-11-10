namespace FlightBooking.Services
{
    public interface IGeminiAIService
    {
        Task<string> GetAIResponseAsync(string userMessage, int? userId = null, List<(string UserMessage, string AIResponse)>? chatHistory = null, bool isAdmin = false);
    }
}

