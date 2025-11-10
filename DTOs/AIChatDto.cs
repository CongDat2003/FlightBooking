namespace FlightBooking.DTOs
{
    public class AIChatRequestDto
    {
        public int? UserId { get; set; }
        public string UserMessage { get; set; } = string.Empty;
        public string? SessionId { get; set; }
        public bool IsAdmin { get; set; } = false; // Phân biệt admin và customer
    }

    public class AIChatResponseDto
    {
        public int MessageId { get; set; }
        public string UserMessage { get; set; } = string.Empty;
        public string AIResponse { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}

