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

        public async Task<string> GetAIResponseAsync(string userMessage, int? userId = null, List<(string UserMessage, string AIResponse)>? chatHistory = null, bool isAdmin = false)
        {
            try
            {
                _logger.LogInformation("Getting AI response for message: {Message}, UserId: {UserId}, IsAdmin: {IsAdmin}", userMessage, userId, isAdmin);

                // Lấy context từ database (khác nhau cho admin và customer)
                var databaseContext = await GetDatabaseContextAsync(userId, isAdmin);

                // Tạo prompt với context (khác nhau cho admin và customer)
                var prompt = BuildPrompt(userMessage, databaseContext, chatHistory, isAdmin);
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

        private async Task<string> GetDatabaseContextAsync(int? userId, bool isAdmin = false)
        {
            var contextParts = new List<string>();

            try
            {
                _logger.LogInformation("Starting to fetch database context for AI. UserId: {UserId}", userId);

                // Lấy thông tin chuyến bay (tăng số lượng để có nhiều lựa chọn hơn)
                var flights = await _context.Flights
                    .Include(f => f.Airline)
                    .Include(f => f.DepartureAirport)
                    .Include(f => f.ArrivalAirport)
                    .Where(f => f.DepartureTime >= DateTime.Now && f.Status != "CANCELLED")
                    .OrderBy(f => f.DepartureTime)
                    .Take(30)
                    .ToListAsync();

                _logger.LogInformation("Fetched {Count} flights from database", flights.Count);

                if (flights.Any())
                {
                    contextParts.Add("=== THÔNG TIN CHUYẾN BAY CÓ SẴN ===");
                    contextParts.Add("(Sắp xếp theo thời gian khởi hành sớm nhất)");
                    foreach (var flight in flights)
                    {
                        var duration = flight.ArrivalTime - flight.DepartureTime;
                        var durationStr = $"{(int)duration.TotalHours}h{duration.Minutes}m";
                        contextParts.Add($"- Chuyến bay {flight.FlightNumber} ({flight.Airline?.AirlineName}): " +
                            $"{flight.DepartureAirport?.AirportName} ({flight.DepartureAirport?.AirportCode}) → " +
                            $"{flight.ArrivalAirport?.AirportName} ({flight.ArrivalAirport?.AirportCode}), " +
                            $"Khởi hành: {flight.DepartureTime:dd/MM/yyyy HH:mm}, " +
                            $"Đến: {flight.ArrivalTime:dd/MM/yyyy HH:mm}, " +
                            $"Thời gian bay: {durationStr}, " +
                            $"Giá cơ bản: {flight.BasePrice:N0} VNĐ, " +
                            $"Trạng thái: {flight.Status}");
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

                // Lấy thông tin dịch vụ (mở rộng cho cả admin và customer)
                var meals = await _context.Meals.Take(20).ToListAsync();
                _logger.LogInformation("Fetched {Count} meals from database", meals.Count);
                if (meals.Any())
                {
                    contextParts.Add("\n=== DỊCH VỤ BỮA ĂN & ĐỒ UỐNG ===");
                    foreach (var meal in meals)
                    {
                        var mealType = !string.IsNullOrEmpty(meal.MealType) ? $" ({meal.MealType})" : "";
                        var classInfo = meal.ClassId.HasValue ? $" [Hạng: {GetSeatClassName(meal.ClassId.Value)}]" : "";
                        contextParts.Add($"- {meal.MealName}{mealType}{classInfo}: {meal.Price:N0} VNĐ");
                    }
                }

                var luggages = await _context.Luggages.Take(15).ToListAsync();
                _logger.LogInformation("Fetched {Count} luggages from database", luggages.Count);
                if (luggages.Any())
                {
                    contextParts.Add("\n=== DỊCH VỤ HÀNH LÝ ===");
                    foreach (var luggage in luggages)
                    {
                        var weightInfo = luggage.WeightLimit > 0 ? $" - {luggage.WeightLimit}kg" : "";
                        contextParts.Add($"- {luggage.LuggageName} ({luggage.LuggageType}){weightInfo}: {luggage.Price:N0} VNĐ");
                    }
                }

                var insurances = await _context.Insurances.Take(15).ToListAsync();
                _logger.LogInformation("Fetched {Count} insurances from database", insurances.Count);
                if (insurances.Any())
                {
                    contextParts.Add("\n=== DỊCH VỤ BẢO HIỂM ===");
                    foreach (var insurance in insurances)
                    {
                        var typeInfo = !string.IsNullOrEmpty(insurance.InsuranceType) ? $" ({insurance.InsuranceType})" : "";
                        contextParts.Add($"- {insurance.InsuranceName}{typeInfo}: {insurance.Price:N0} VNĐ");
                    }
                }

                // Thêm thông tin cho admin
                if (isAdmin)
                {
                    // Thống kê tổng quan cho admin
                    var totalBookings = await _context.Bookings.CountAsync();
                    var totalUsers = await _context.Users.Where(u => u.Role == "Customer").CountAsync();
                    var totalFlights = await _context.Flights.CountAsync();
                    var pendingBookings = await _context.Bookings.Where(b => b.BookingStatus == "PENDING").CountAsync();
                    var confirmedBookings = await _context.Bookings.Where(b => b.BookingStatus == "CONFIRMED").CountAsync();
                    
                    contextParts.Add("\n=== THỐNG KÊ HỆ THỐNG (CHO ADMIN) ===");
                    contextParts.Add($"- Tổng số đặt chỗ: {totalBookings}");
                    contextParts.Add($"- Tổng số khách hàng: {totalUsers}");
                    contextParts.Add($"- Tổng số chuyến bay: {totalFlights}");
                    contextParts.Add($"- Đặt chỗ đang chờ: {pendingBookings}");
                    contextParts.Add($"- Đặt chỗ đã xác nhận: {confirmedBookings}");
                    
                    // Thông tin đặt chỗ gần đây cho admin
                    var recentBookings = await _context.Bookings
                        .Include(b => b.Flight)
                        .OrderByDescending(b => b.BookingDate)
                        .Take(10)
                        .ToListAsync();
                    
                    if (recentBookings.Any())
                    {
                        contextParts.Add("\n=== ĐẶT CHỖ GẦN ĐÂY (CHO ADMIN) ===");
                        foreach (var booking in recentBookings)
                        {
                            contextParts.Add($"- Mã: {booking.BookingReference}, " +
                                $"Chuyến bay: {booking.Flight?.FlightNumber}, " +
                                $"Trạng thái: {booking.BookingStatus}, " +
                                $"Thanh toán: {booking.PaymentStatus}, " +
                                $"Tổng: {booking.TotalAmount:N0} VNĐ");
                        }
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

        private string GetSeatClassName(int classId)
        {
            return classId switch
            {
                1 => "Hạng Phổ Thông",
                2 => "Hạng Thương Gia",
                3 => "Hạng Nhất",
                _ => "Không xác định"
            };
        }

        private string BuildPrompt(string userMessage, string databaseContext, List<(string UserMessage, string AIResponse)>? chatHistory, bool isAdmin = false)
        {
            var systemPrompt = isAdmin 
                ? BuildAdminPrompt(userMessage, databaseContext, chatHistory)
                : BuildCustomerPrompt(userMessage, databaseContext, chatHistory);
            
            return systemPrompt;
        }

        private string BuildCustomerPrompt(string userMessage, string databaseContext, List<(string UserMessage, string AIResponse)>? chatHistory)
        {
            var systemPrompt = @"Bạn là trợ lý AI thông minh và thân thiện của hệ thống đặt vé máy bay. Nhiệm vụ của bạn là hỗ trợ KHÁCH HÀNG:

1. TRẢ LỜI CÁC CÂU HỎI VỀ:
   - Tìm kiếm và đặt chuyến bay (điểm đi, điểm đến, ngày giờ, giá cả, hạng ghế)
   - Thông tin về các hãng hàng không, sân bay, thời gian bay
   - Dịch vụ bổ sung:
     * Bữa ăn & Đồ uống: các loại món ăn, đồ uống, giá cả, hạng ghế tương ứng
     * Hành lý: trọng lượng, loại hành lý, giá cả
     * Bảo hiểm: các gói bảo hiểm, mức độ bảo vệ, giá cả
   - Thanh toán: các phương thức thanh toán (VNPay, MoMo, ZaloPay, QR Code)
   - Trạng thái đặt chỗ, hủy vé, đổi vé, hoàn tiền
   - Hướng dẫn sử dụng hệ thống, cách đặt vé, cách thanh toán
   - Câu hỏi về chính sách: chính sách hủy vé, đổi vé, hoàn tiền
   - Câu hỏi về dịch vụ: dịch vụ miễn phí, dịch vụ trả phí, combo dịch vụ

2. PHONG CÁCH TRẢ LỜI:
   - Luôn lịch sự, thân thiện và chuyên nghiệp
   - Trả lời bằng tiếng Việt, ngắn gọn nhưng đầy đủ thông tin
   - Sử dụng ngôn ngữ tự nhiên, dễ hiểu
   - Nếu có thể, đưa ra nhiều lựa chọn cho khách hàng
   - Hỏi thêm thông tin nếu cần thiết để hỗ trợ tốt hơn
   - Đưa ra gợi ý hữu ích dựa trên nhu cầu

3. XỬ LÝ THÔNG TIN:
   - Ưu tiên sử dụng thông tin từ database được cung cấp
   - Nếu không có thông tin trong database, hãy nói rõ và hướng dẫn khách hàng cách tìm kiếm hoặc liên hệ hỗ trợ
   - Đề xuất các chuyến bay phù hợp dựa trên yêu cầu
   - So sánh giá cả và dịch vụ khi có nhiều lựa chọn
   - Giải thích rõ về các hạng ghế (Phổ Thông, Thương Gia, Nhất) và dịch vụ đi kèm

4. TƯƠNG TÁC:
   - Nhớ thông tin từ các câu hỏi trước trong cuộc trò chuyện
   - Đưa ra gợi ý hữu ích dựa trên ngữ cảnh
   - Hỏi lại để làm rõ nếu câu hỏi không rõ ràng

THÔNG TIN TỪ DATABASE:
" + databaseContext + @"

LỊCH SỬ TRÒ CHUYỆN:";

            if (chatHistory != null && chatHistory.Any())
            {
                systemPrompt += "\n\nCác câu hỏi và trả lời trước đó:";
                foreach (var (userMsg, aiResp) in chatHistory.TakeLast(5))
                {
                    systemPrompt += $"\n\nKhách hàng: {userMsg}";
                    systemPrompt += $"\nAI: {aiResp}";
                }
            }

            systemPrompt += $"\n\nCÂU HỎI HIỆN TẠI CỦA KHÁCH HÀNG: {userMessage}";
            systemPrompt += "\n\nHãy trả lời câu hỏi của khách hàng một cách thân thiện, chi tiết và hữu ích. Nếu cần thêm thông tin, hãy hỏi lại một cách lịch sự. Sử dụng thông tin từ database và lịch sử trò chuyện để đưa ra câu trả lời chính xác nhất.";

            return systemPrompt;
        }

        private string BuildAdminPrompt(string userMessage, string databaseContext, List<(string UserMessage, string AIResponse)>? chatHistory)
        {
            var systemPrompt = @"Bạn là trợ lý AI thông minh và chuyên nghiệp của hệ thống quản lý đặt vé máy bay. Nhiệm vụ của bạn là hỗ trợ ADMIN trong việc quản lý hệ thống:

1. TRẢ LỜI CÁC CÂU HỎI VỀ QUẢN LÝ HỆ THỐNG:
   - Quản lý chuyến bay: tạo, sửa, xóa chuyến bay, quản lý ghế, trạng thái chuyến bay
   - Quản lý đặt chỗ: xem danh sách đặt chỗ, cập nhật trạng thái, xác nhận/hủy đặt chỗ
   - Quản lý khách hàng: xem danh sách khách hàng, thông tin khách hàng, lịch sử đặt chỗ
   - Quản lý dịch vụ: quản lý bữa ăn, hành lý, bảo hiểm, giá cả
   - Thống kê và báo cáo: số lượng đặt chỗ, doanh thu, chuyến bay phổ biến
   - Xử lý sự cố: chuyến bay bị hoãn, hủy, thay đổi lịch trình
   - Quản lý thanh toán: theo dõi trạng thái thanh toán, hoàn tiền

2. PHONG CÁCH TRẢ LỜI:
   - Chuyên nghiệp, chính xác và rõ ràng
   - Trả lời bằng tiếng Việt, ngắn gọn nhưng đầy đủ thông tin
   - Đưa ra hướng dẫn cụ thể về các thao tác quản lý
   - Gợi ý các bước xử lý khi có vấn đề

3. XỬ LÝ THÔNG TIN:
   - Ưu tiên sử dụng thông tin từ database được cung cấp
   - Đưa ra thống kê và phân tích dựa trên dữ liệu
   - Gợi ý các hành động quản lý phù hợp
   - Cảnh báo về các vấn đề cần chú ý (ví dụ: đặt chỗ đang chờ xử lý)

4. TƯƠNG TÁC:
   - Nhớ thông tin từ các câu hỏi trước trong cuộc trò chuyện
   - Đưa ra gợi ý quản lý dựa trên ngữ cảnh
   - Hỏi lại để làm rõ nếu câu hỏi không rõ ràng

THÔNG TIN TỪ DATABASE:
" + databaseContext + @"

LỊCH SỬ TRÒ CHUYỆN:";

            if (chatHistory != null && chatHistory.Any())
            {
                systemPrompt += "\n\nCác câu hỏi và trả lời trước đó:";
                foreach (var (userMsg, aiResp) in chatHistory.TakeLast(5))
                {
                    systemPrompt += $"\n\nAdmin: {userMsg}";
                    systemPrompt += $"\nAI: {aiResp}";
                }
            }

            systemPrompt += $"\n\nCÂU HỎI HIỆN TẠI CỦA ADMIN: {userMessage}";
            systemPrompt += "\n\nHãy trả lời câu hỏi của admin một cách chuyên nghiệp, chi tiết và hữu ích. Đưa ra hướng dẫn cụ thể về các thao tác quản lý. Sử dụng thông tin từ database và lịch sử trò chuyện để đưa ra câu trả lời chính xác nhất.";

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

