namespace FlightBooking.Models
{
    public class AIChatMessage
    {
        public int MessageId { get; set; }
        public int? UserId { get; set; }
        public string UserMessage { get; set; } = string.Empty;
        public string AIResponse { get; set; } = string.Empty;
        public string? SessionId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation property
        public User? User { get; set; }
    }
}






























