namespace FlightBooking.DTOs
{
    public class CreateMessageDto
    {
        public int? UserId { get; set; }
        public string Content { get; set; }
        public string SenderType { get; set; } = "USER"; // USER, ADMIN
    }

    public class MessageResponseDto
    {
        public int MessageId { get; set; }
        public int? UserId { get; set; }
        public string UserName { get; set; }
        public string Content { get; set; }
        public string SenderType { get; set; }
        public string Status { get; set; }
        public bool IsAutoReply { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }

    public class ChatConversationDto
    {
        public List<MessageResponseDto> Messages { get; set; } = new List<MessageResponseDto>();
        public int UnreadCount { get; set; }
        public DateTime? LastMessageTime { get; set; }
    }

    public class AdminMessageResponseDto : MessageResponseDto
    {
        public string UserEmail { get; set; }
    }

    public class AdminReplyDto
    {
        public int UserId { get; set; }
        public string Content { get; set; }
    }
}

