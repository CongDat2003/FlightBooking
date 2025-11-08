using FlightBooking.Configuration;
using FlightBooking.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace FlightBooking.Services
{
    public class GeminiAIService : IGeminiAIService
    {
        private readonly GeminiSettings _settings;
        private readonly FlightBookingContext _context;
        private readonly ILogger<GeminiAIService> _logger;

        public GeminiAIService(
            IOptions<GeminiSettings> settings,
            FlightBookingContext context,
            ILogger<GeminiAIService> logger)
        {
            _settings = settings.Value;
            _context = context;
            _logger = logger;
        }

        public async Task<string> GetAIResponseAsync(string userMessage, int? userId = null, List<(string UserMessage, string AIResponse)>? chatHistory = null)
        {
            try
            {
                _logger.LogInformation("Getting AI response for message: {Message}, UserId: {UserId}", userMessage, userId);

                // Lấy context từ database
                var databaseContext = await GetDatabaseContextAsync(userId);

                // Tạo prompt với context
                var prompt = BuildPrompt(userMessage, databaseContext, chatHistory);
                _logger.LogDebug("Prompt built. Prompt length: {Length} characters", prompt.Length);

                // Gọi Gemini API qua REST
                var aiResponse = await CallGeminiAPIAsync(prompt);

                _logger.LogInformation("AI Response generated successfully. Response length: {Length} characters", aiResponse?.Length ?? 0);
                return aiResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI response: {Message}", ex.Message);
                _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
                return "Xin lỗi, đã có lỗi xảy ra khi xử lý câu hỏi của bạn. Vui lòng thử lại sau hoặc liên hệ với admin để được hỗ trợ.";
            }
        }

        private async Task<string> GetDatabaseContextAsync(int? userId)
        {
            var contextParts = new List<string>();

            try
            {
                _logger.LogInformation("Starting to fetch database context for AI. UserId: {UserId}", userId);

                // Lấy thông tin chuyến bay
                var flights = await _context.Flights
                    .Include(f => f.Airline)
                    .Include(f => f.DepartureAirport)
                    .Include(f => f.ArrivalAirport)
                    .Where(f => f.DepartureTime >= DateTime.Now)
                    .OrderBy(f => f.DepartureTime)
                    .Take(20)
                    .ToListAsync();

                _logger.LogInformation("Fetched {Count} flights from database", flights.Count);

                if (flights.Any())
                {
                    contextParts.Add("=== THÔNG TIN CHUYẾN BAY CÓ SẴN ===");
                    foreach (var flight in flights)
                    {
                        contextParts.Add($"- Chuyến bay {flight.FlightNumber} ({flight.Airline?.AirlineName}): " +
                            $"{flight.DepartureAirport?.AirportName} ({flight.DepartureAirport?.AirportCode}) → " +
                            $"{flight.ArrivalAirport?.AirportName} ({flight.ArrivalAirport?.AirportCode}), " +
                            $"Khởi hành: {flight.DepartureTime:dd/MM/yyyy HH:mm}, " +
                            $"Đến: {flight.ArrivalTime:dd/MM/yyyy HH:mm}, " +
                            $"Giá: {flight.BasePrice:N0} VNĐ");
                    }
                }

                // Lấy thông tin sân bay
                var airports = await _context.Airports
                    .Take(30)
                    .ToListAsync();

                _logger.LogInformation("Fetched {Count} airports from database", airports.Count);

                if (airports.Any())
                {
                    contextParts.Add("\n=== DANH SÁCH SÂN BAY ===");
                    foreach (var airport in airports)
                    {
                        contextParts.Add($"- {airport.AirportName} ({airport.AirportCode}) - {airport.City}, {airport.Country}");
                    }
                }

                // Lấy thông tin hãng hàng không
                var airlines = await _context.Airlines
                    .Take(20)
                    .ToListAsync();

                _logger.LogInformation("Fetched {Count} airlines from database", airlines.Count);

                if (airlines.Any())
                {
                    contextParts.Add("\n=== DANH SÁCH HÃNG HÀNG KHÔNG ===");
                    foreach (var airline in airlines)
                    {
                        contextParts.Add($"- {airline.AirlineName} ({airline.AirlineCode})");
                    }
                }

                // Nếu có userId, lấy thông tin đặt chỗ của user
                if (userId.HasValue)
                {
                    var userBookings = await _context.Bookings
                        .Include(b => b.Flight)
                            .ThenInclude(f => f.Airline)
                        .Include(b => b.Flight)
                            .ThenInclude(f => f.DepartureAirport)
                        .Include(b => b.Flight)
                            .ThenInclude(f => f.ArrivalAirport)
                        .Where(b => b.UserId == userId.Value)
                        .OrderByDescending(b => b.BookingDate)
                        .Take(10)
                        .ToListAsync();

                    _logger.LogInformation("Fetched {Count} bookings for userId {UserId}", userBookings.Count, userId.Value);

                    if (userBookings.Any())
                    {
                        contextParts.Add("\n=== ĐẶT CHỖ CỦA KHÁCH HÀNG ===");
                        foreach (var booking in userBookings)
                        {
                            contextParts.Add($"- Mã đặt chỗ: {booking.BookingReference}, " +
                                $"Chuyến bay: {booking.Flight?.FlightNumber}, " +
                                $"Trạng thái: {booking.BookingStatus}, " +
                                $"Thanh toán: {booking.PaymentStatus}, " +
                                $"Tổng tiền: {booking.TotalAmount:N0} VNĐ, " +
                                $"Ngày đặt: {booking.BookingDate:dd/MM/yyyy}");
                        }
                    }
                }

                // Lấy thông tin dịch vụ
                var meals = await _context.Meals.Take(10).ToListAsync();
                _logger.LogInformation("Fetched {Count} meals from database", meals.Count);
                if (meals.Any())
                {
                    contextParts.Add("\n=== DỊCH VỤ BỮA ĂN ===");
                    foreach (var meal in meals)
                    {
                        contextParts.Add($"- {meal.MealName}: {meal.Price:N0} VNĐ");
                    }
                }

                var luggages = await _context.Luggages.Take(10).ToListAsync();
                _logger.LogInformation("Fetched {Count} luggages from database", luggages.Count);
                if (luggages.Any())
                {
                    contextParts.Add("\n=== DỊCH VỤ HÀNH LÝ ===");
                    foreach (var luggage in luggages)
                    {
                        contextParts.Add($"- {luggage.LuggageType}: {luggage.Price:N0} VNĐ");
                    }
                }

                var insurances = await _context.Insurances.Take(10).ToListAsync();
                _logger.LogInformation("Fetched {Count} insurances from database", insurances.Count);
                if (insurances.Any())
                {
                    contextParts.Add("\n=== DỊCH VỤ BẢO HIỂM ===");
                    foreach (var insurance in insurances)
                    {
                        contextParts.Add($"- {insurance.InsuranceName}: {insurance.Price:N0} VNĐ");
                    }
                }

                var contextString = string.Join("\n", contextParts);
                _logger.LogInformation("Database context built successfully. Context length: {Length} characters", contextString.Length);
                _logger.LogDebug("Database context content: {Context}", contextString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building database context: {Message}", ex.Message);
                _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
            }

            return string.Join("\n", contextParts);
        }

        private string BuildPrompt(string userMessage, string databaseContext, List<(string UserMessage, string AIResponse)>? chatHistory)
        {
            var systemPrompt = @"Bạn là trợ lý AI thông minh của hệ thống đặt vé máy bay. Nhiệm vụ của bạn là:
1. Trả lời các câu hỏi về chuyến bay, đặt chỗ, thanh toán một cách chính xác và thân thiện
2. Sử dụng thông tin từ database được cung cấp để trả lời
3. Nếu không có thông tin trong database, hãy nói rõ và hướng dẫn khách hàng liên hệ admin
4. Trả lời bằng tiếng Việt, ngắn gọn, dễ hiểu
5. Luôn lịch sự và chuyên nghiệp

THÔNG TIN TỪ DATABASE:
" + databaseContext + @"

LỊCH SỬ TRÒ CHUYỆN:";

            if (chatHistory != null && chatHistory.Any())
            {
                foreach (var (userMsg, aiResp) in chatHistory.TakeLast(5))
                {
                    systemPrompt += $"\nKhách hàng: {userMsg}";
                    systemPrompt += $"\nAI: {aiResp}";
                }
            }

            systemPrompt += $"\n\nCÂU HỎI HIỆN TẠI CỦA KHÁCH HÀNG: {userMessage}";
            systemPrompt += "\n\nHãy trả lời câu hỏi của khách hàng dựa trên thông tin từ database và lịch sử trò chuyện:";

            return systemPrompt;
        }

        private async Task<string> CallGeminiAPIAsync(string prompt)
        {
            try
            {
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Model cũ (gemini-pro) dùng v1, model mới (gemini-1.5-*, gemini-2-*) dùng v1beta
                var apiVersion = (_settings.Model.StartsWith("gemini-1.5") || _settings.Model.StartsWith("gemini-2")) ? "v1beta" : "v1";
                var fullUrl = $"https://generativelanguage.googleapis.com/{apiVersion}/models/{_settings.Model}:generateContent?key={_settings.ApiKey}";
                
                _logger.LogInformation("Using API version: {Version} for model: {Model}", apiVersion, _settings.Model);
                _logger.LogInformation("Calling Gemini API: {Url}", fullUrl);
                _logger.LogDebug("Request body: {Body}", json);

                // Tạo HttpClient mới không dùng BaseAddress
                using var httpClient = new HttpClient();
                var response = await httpClient.PostAsync(fullUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Gemini API response status: {StatusCode}", response.StatusCode);
                _logger.LogDebug("Gemini API response: {Response}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = JsonDocument.Parse(responseContent);
                    
                    if (jsonDoc.RootElement.TryGetProperty("candidates", out var candidates) && 
                        candidates.GetArrayLength() > 0)
                    {
                        var firstCandidate = candidates[0];
                        
                        // Kiểm tra finishReason nếu có
                        if (firstCandidate.TryGetProperty("finishReason", out var finishReason))
                        {
                            var reason = finishReason.GetString();
                            if (reason != "STOP" && reason != null)
                            {
                                _logger.LogWarning("Gemini API finish reason: {Reason}", reason);
                            }
                        }
                        
                        if (firstCandidate.TryGetProperty("content", out var contentElement) &&
                            contentElement.TryGetProperty("parts", out var parts))
                        {
                            if (parts.GetArrayLength() > 0)
                            {
                                var firstPart = parts[0];
                                if (firstPart.TryGetProperty("text", out var textElement))
                                {
                                    var result = textElement.GetString();
                                    if (!string.IsNullOrWhiteSpace(result))
                                    {
                                        _logger.LogInformation("Successfully received AI response");
                                        return result;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Gemini API returned empty candidates array");
                    }
                }
                else
                {
                    _logger.LogError("Gemini API error: {StatusCode} - {Error}", response.StatusCode, responseContent);
                    
                    // Try to parse error message from response
                    try
                    {
                        var errorDoc = JsonDocument.Parse(responseContent);
                        if (errorDoc.RootElement.TryGetProperty("error", out var errorElement))
                        {
                            if (errorElement.TryGetProperty("message", out var messageElement))
                            {
                                var errorMessage = messageElement.GetString();
                                _logger.LogError("Gemini API error message: {Message}", errorMessage);
                                
                                // Trả về thông báo lỗi cụ thể hơn
                                if (errorMessage != null && errorMessage.Contains("API key"))
                                {
                                    return "Xin lỗi, có lỗi xác thực API. Vui lòng liên hệ admin.";
                                }
                                if (errorMessage != null && errorMessage.Contains("model"))
                                {
                                    return "Xin lỗi, model không hợp lệ. Vui lòng liên hệ admin.";
                                }
                            }
                        }
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogError(parseEx, "Error parsing error response");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API: {Message}", ex.Message);
                _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
            }

            return "Xin lỗi, tôi không thể trả lời câu hỏi này lúc này. Vui lòng thử lại sau.";
        }
    }
}

