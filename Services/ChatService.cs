using FlightBooking.DTOs;
using FlightBooking.Models;
using Microsoft.EntityFrameworkCore;


namespace FlightBooking.Services
{
    public class ChatService : IChatService
    {
        private readonly FlightBookingContext _context;
        private readonly ILogger<ChatService> _logger;

        public ChatService(FlightBookingContext context, ILogger<ChatService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<MessageResponseDto> SendMessageAsync(CreateMessageDto messageDto)
        {
            var message = new Message
            {
                UserId = messageDto.UserId,
                Content = messageDto.Content,
                SenderType = messageDto.SenderType,
                Status = "SENT",
                CreatedAt = DateTime.Now
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Nếu user gửi tin nhắn và chưa có admin trả lời trong 30 giây, tự động gửi auto-reply
            if (messageDto.SenderType == "USER" && messageDto.UserId.HasValue)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(30000); // Đợi 30 giây
                    var hasAdminReply = await _context.Messages
                        .AnyAsync(m => m.UserId == messageDto.UserId 
                            && m.SenderType == "ADMIN" 
                            && m.CreatedAt > message.CreatedAt);
                    
                    if (!hasAdminReply)
                    {
                        var autoReply = new Message
                        {
                            UserId = messageDto.UserId,
                            Content = "Xin chào! Cảm ơn bạn đã liên hệ. Chúng tôi đã nhận được tin nhắn của bạn và sẽ phản hồi sớm nhất có thể. Vui lòng đợi một chút nhé!",
                            SenderType = "SYSTEM",
                            Status = "SENT",
                            IsAutoReply = true,
                            CreatedAt = DateTime.Now
                        };
                        _context.Messages.Add(autoReply);
                        await _context.SaveChangesAsync();
                    }
                });
            }

            return await GetMessageDtoAsync(message);
        }

        public async Task<ChatConversationDto> GetConversationAsync(int? userId = null)
        {
            try
            {
                var query = _context.Messages
                    .Include(m => m.User)
                    .OrderBy(m => m.CreatedAt)
                    .AsQueryable();

                if (userId.HasValue && userId.Value > 0)
                {
                    // Chỉ lấy tin nhắn của user này (cả USER, ADMIN, SYSTEM cho user này)
                    // Lọc chính xác theo userId để user chỉ thấy tin nhắn của chính họ
                    query = query.Where(m => m.UserId.HasValue && m.UserId.Value == userId.Value);
                    _logger.LogInformation("Filtering messages for userId: {UserId}", userId.Value);
                }
                else
                {
                    // Nếu không có userId, lấy tất cả tin nhắn (cho admin)
                    // Không filter
                    _logger.LogInformation("Loading all messages for admin");
                }

                var messages = await query.ToListAsync();
                
                _logger.LogInformation("Found {Count} messages for userId: {UserId}", messages.Count, userId);

                // Unread count: với user thì đếm tin nhắn từ ADMIN/SYSTEM chưa đọc
                // Với admin thì đếm tin nhắn từ USER chưa đọc
                int unreadCount;
                if (userId.HasValue && userId.Value > 0)
                {
                    unreadCount = messages.Count(m => m.Status == "SENT" && (m.SenderType == "ADMIN" || m.SenderType == "SYSTEM"));
                }
                else
                {
                    unreadCount = messages.Count(m => m.Status == "SENT" && m.SenderType == "USER");
                }

                return new ChatConversationDto
                {
                    Messages = messages.Select(m => new MessageResponseDto
                    {
                        MessageId = m.MessageId,
                        UserId = m.UserId,
                        UserName = m.User?.FullName ?? (m.SenderType == "ADMIN" ? "Admin" : (m.SenderType == "SYSTEM" ? "Hệ thống" : "User")),
                        Content = m.Content ?? string.Empty,
                        SenderType = m.SenderType ?? "USER",
                        Status = m.Status ?? "SENT",
                        IsAutoReply = m.IsAutoReply,
                        CreatedAt = m.CreatedAt,
                        ReadAt = m.ReadAt
                    }).ToList(),
                    UnreadCount = unreadCount,
                    LastMessageTime = messages.LastOrDefault()?.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetConversationAsync for userId: {UserId}", userId);
                throw;
            }
        }

        public async Task<MessageResponseDto> MarkAsReadAsync(int messageId)
        {
            var message = await _context.Messages.FindAsync(messageId);
            if (message == null)
                throw new ArgumentException("Message not found");

            message.Status = "READ";
            message.ReadAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return await GetMessageDtoAsync(message);
        }

        public async Task<List<MessageResponseDto>> GetUnreadMessagesAsync(int? userId = null)
        {
            var query = _context.Messages
                .Include(m => m.User)
                .Where(m => m.Status == "SENT")
                .AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(m => m.UserId == userId);
            }
            else
            {
                query = query.Where(m => m.SenderType == "USER");
            }

            var messages = await query
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return messages.Select(m => new MessageResponseDto
            {
                MessageId = m.MessageId,
                UserId = m.UserId,
                UserName = m.User?.FullName ?? "User",
                Content = m.Content,
                SenderType = m.SenderType,
                Status = m.Status,
                IsAutoReply = m.IsAutoReply,
                CreatedAt = m.CreatedAt,
                ReadAt = m.ReadAt
            }).ToList();
        }

        public async Task<MessageResponseDto> SendAdminReplyAsync(int messageId, string content)
        {
            var originalMessage = await _context.Messages
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.MessageId == messageId);

            if (originalMessage == null)
                throw new ArgumentException("Original message not found");

            var reply = new Message
            {
                UserId = originalMessage.UserId,
                Content = content,
                SenderType = "ADMIN",
                Status = "SENT",
                CreatedAt = DateTime.Now
            };

            _context.Messages.Add(reply);
            
            // Đánh dấu tin nhắn gốc đã đọc nếu chưa
            if (originalMessage.Status == "SENT")
            {
                originalMessage.Status = "READ";
                originalMessage.ReadAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return await GetMessageDtoAsync(reply);
        }

        public async Task<List<AdminMessageResponseDto>> GetUnreadMessagesForAdminAsync()
        {
            try
            {
                var messages = await _context.Messages
                    .Include(m => m.User)
                    .Where(m => m.SenderType == "USER" && m.Status == "SENT" && m.UserId != null)
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync();

                return messages.Select(m => new AdminMessageResponseDto
                {
                    MessageId = m.MessageId,
                    UserId = m.UserId.HasValue ? m.UserId.Value : 0,
                    UserName = m.User?.FullName ?? "User",
                    UserEmail = m.User?.Email ?? string.Empty,
                    Content = m.Content ?? string.Empty,
                    SenderType = m.SenderType ?? "USER",
                    Status = m.Status ?? "SENT",
                    IsAutoReply = m.IsAutoReply,
                    CreatedAt = m.CreatedAt,
                    ReadAt = m.ReadAt
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUnreadMessagesForAdminAsync");
                throw;
            }
        }

        public async Task MarkMessagesAsReadAsync(int userId)
        {
            var unreadMessages = await _context.Messages
                .Where(m => m.UserId == userId && m.SenderType == "ADMIN" && m.Status == "SENT")
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.Status = "READ";
                message.ReadAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        // Overload for SendAdminReplyAsync with userId
        public async Task<MessageResponseDto> SendAdminReplyByUserIdAsync(int userId, string content)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new ArgumentException("User not found");

            var reply = new Message
            {
                UserId = userId,
                Content = content,
                SenderType = "ADMIN",
                Status = "SENT",
                CreatedAt = DateTime.Now
            };

            _context.Messages.Add(reply);
            await _context.SaveChangesAsync();

            return await GetMessageDtoAsync(reply);
        }

        private async Task<MessageResponseDto> GetMessageDtoAsync(Message message)
        {
            if (message.UserId.HasValue && message.User == null)
            {
                await _context.Entry(message)
                    .Reference(m => m.User)
                    .LoadAsync();
            }

            return new MessageResponseDto
            {
                MessageId = message.MessageId,
                UserId = message.UserId,
                UserName = message.User?.FullName ?? (message.SenderType == "ADMIN" ? "Admin" : "User"),
                Content = message.Content,
                SenderType = message.SenderType,
                Status = message.Status,
                IsAutoReply = message.IsAutoReply,
                CreatedAt = message.CreatedAt,
                ReadAt = message.ReadAt
            };
        }
    }
}

