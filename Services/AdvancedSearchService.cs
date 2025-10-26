using FlightBooking.DTOs;
using FlightBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Services
{
    public class AdvancedSearchService : IAdvancedSearchService
    {
        private readonly FlightBookingContext _context;

        public AdvancedSearchService(FlightBookingContext context)
        {
            _context = context;
        }

        public async Task<FlightSearchResultDto> AdvancedSearchAsync(AdvancedFlightSearchDto searchDto)
        {
            var query = _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .Include(f => f.Seats)
                .Where(f => f.DepartureAirport.AirportCode == searchDto.DepartureAirportCode
                         && f.ArrivalAirport.AirportCode == searchDto.ArrivalAirportCode
                         && f.DepartureTime.Date == searchDto.DepartureDate.Date
                         && f.Status == "SCHEDULED");

            // Apply filters
            if (searchDto.Airlines?.Any() == true)
            {
                query = query.Where(f => searchDto.Airlines.Contains(f.Airline.AirlineCode));
            }

            if (searchDto.DepartureTimeFrom.HasValue)
            {
                query = query.Where(f => f.DepartureTime.TimeOfDay >= searchDto.DepartureTimeFrom.Value);
            }

            if (searchDto.DepartureTimeTo.HasValue)
            {
                query = query.Where(f => f.DepartureTime.TimeOfDay <= searchDto.DepartureTimeTo.Value);
            }

            if (searchDto.MinPrice.HasValue)
            {
                query = query.Where(f => f.BasePrice >= searchDto.MinPrice.Value);
            }

            if (searchDto.MaxPrice.HasValue)
            {
                query = query.Where(f => f.BasePrice <= searchDto.MaxPrice.Value);
            }

            // Apply sorting
            query = searchDto.SortBy.ToUpper() switch
            {
                "PRICE" => searchDto.SortOrder == "DESC"
                    ? query.OrderByDescending(f => f.BasePrice)
                    : query.OrderBy(f => f.BasePrice),
                "DEPARTURE_TIME" => searchDto.SortOrder == "DESC"
                    ? query.OrderByDescending(f => f.DepartureTime)
                    : query.OrderBy(f => f.DepartureTime),
                "DURATION" => searchDto.SortOrder == "DESC"
                    ? query.OrderByDescending(f => f.ArrivalTime.Subtract(f.DepartureTime))
                    : query.OrderBy(f => f.ArrivalTime.Subtract(f.DepartureTime)),
                _ => query.OrderBy(f => f.BasePrice)
            };

            var flights = await query.ToListAsync();

            var outboundFlights = flights.Select(f => new FlightResponseDto
            {
                FlightId = f.FlightId,
                FlightNumber = f.FlightNumber,
                AirlineName = f.Airline.AirlineName,
                AirlineCode = f.Airline.AirlineCode,
                DepartureAirport = $"{f.DepartureAirport.AirportName} ({f.DepartureAirport.AirportCode})",
                ArrivalAirport = $"{f.ArrivalAirport.AirportName} ({f.ArrivalAirport.AirportCode})",
                DepartureTime = f.DepartureTime,
                ArrivalTime = f.ArrivalTime,
                BasePrice = f.BasePrice,
                Status = f.Status,
                Gate = f.Gate,
                AvailableSeats = f.Seats.Count(s => s.IsAvailable ?? false)
            }).ToList();

            // Generate metadata
            var metadata = new SearchMetadataDto
            {
                TotalResults = outboundFlights.Count,
                MinPrice = outboundFlights.Any() ? outboundFlights.Min(f => f.BasePrice) : 0,
                MaxPrice = outboundFlights.Any() ? outboundFlights.Max(f => f.BasePrice) : 0,
                AvailableAirlines = outboundFlights.Select(f => f.AirlineName).Distinct().ToList(),
                DepartureTimeSlots = GenerateTimeSlots(outboundFlights)
            };

            var result = new FlightSearchResultDto
            {
                OutboundFlights = outboundFlights,
                Metadata = metadata
            };

            // Handle return flights if round trip
            if (searchDto.ReturnDate.HasValue)
            {
                var returnSearchDto = new AdvancedFlightSearchDto
                {
                    DepartureAirportCode = searchDto.ArrivalAirportCode,
                    ArrivalAirportCode = searchDto.DepartureAirportCode,
                    DepartureDate = searchDto.ReturnDate.Value,
                    Passengers = searchDto.Passengers,
                    SeatClass = searchDto.SeatClass,
                    Airlines = searchDto.Airlines,
                    SortBy = searchDto.SortBy,
                    SortOrder = searchDto.SortOrder
                };

                var returnResult = await AdvancedSearchAsync(returnSearchDto);
                result.ReturnFlights = returnResult.OutboundFlights;
            }

            return result;
        }

        private List<TimeSlotDto> GenerateTimeSlots(List<FlightResponseDto> flights)
        {
            var timeSlots = new List<TimeSlotDto>
            {
                new TimeSlotDto { Label = "Sáng sớm (00:00 - 06:00)", From = TimeSpan.Zero, To = new TimeSpan(6, 0, 0) },
                new TimeSlotDto { Label = "Buổi sáng (06:00 - 12:00)", From = new TimeSpan(6, 0, 0), To = new TimeSpan(12, 0, 0) },
                new TimeSlotDto { Label = "Buổi chiều (12:00 - 18:00)", From = new TimeSpan(12, 0, 0), To = new TimeSpan(18, 0, 0) },
                new TimeSlotDto { Label = "Buổi tối (18:00 - 24:00)", From = new TimeSpan(18, 0, 0), To = new TimeSpan(24, 0, 0) }
            };

            foreach (var slot in timeSlots)
            {
                slot.FlightCount = flights.Count(f =>
                    f.DepartureTime.TimeOfDay >= slot.From &&
                    f.DepartureTime.TimeOfDay < slot.To);
            }

            return timeSlots;
        }

        public async Task<List<string>> GetPopularDestinationsAsync(string fromAirport)
        {
            var popularDestinations = await _context.Bookings
                .Include(b => b.Flight)
                    .ThenInclude(f => f.ArrivalAirport)
                .Where(b => b.Flight.DepartureAirport.AirportCode == fromAirport
                         && b.BookingStatus == "CONFIRMED"
                         && b.BookingDate >= DateTime.Now.AddMonths(-6))
                .GroupBy(b => b.Flight.ArrivalAirport.AirportCode)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => g.Key)
                .ToListAsync();

            return popularDestinations;
        }

        public async Task<List<FlightResponseDto>> GetRecommendedFlightsAsync(int userId)
        {
            // Lấy tất cả flights trước, sau đó filter trên client
            var allFlights = await _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .Include(f => f.Seats)
                .Where(f => f.Status == "SCHEDULED" && f.DepartureTime > DateTime.Now)
                .OrderBy(f => f.DepartureTime)
                .Take(100) // Giới hạn để không lấy quá nhiều
                .ToListAsync(); // Chuyển về client side

            // Get user preferences
            var userBookings = await _context.Bookings
                .Include(b => b.Flight)
                    .ThenInclude(f => f.DepartureAirport)
                .Include(b => b.Flight)
                    .ThenInclude(f => f.ArrivalAirport)
                .Include(b => b.Flight.Airline)
                .Where(b => b.UserId == userId && b.BookingStatus == "CONFIRMED")
                .ToListAsync();

            if (!userBookings.Any())
                return allFlights.Take(20).Select(f => MapToFlightResponseDto(f)).ToList();

            // Filter trên client side
            var preferredAirlines = userBookings
                .GroupBy(b => b.Flight.Airline.AirlineCode)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.Key)
                .ToHashSet();

            var frequentRoutes = userBookings
                .GroupBy(b => new {
                    From = b.Flight.DepartureAirport.AirportCode,
                    To = b.Flight.ArrivalAirport.AirportCode
                })
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => (g.Key.From, g.Key.To))
                .ToHashSet();

            // Filter flights trên client
            var recommendedFlights = allFlights
                .Where(f => preferredAirlines.Contains(f.Airline.AirlineCode) ||
                           frequentRoutes.Contains((f.DepartureAirport.AirportCode, f.ArrivalAirport.AirportCode)))
                .Take(20)
                .ToList();

            return recommendedFlights.Select(f => MapToFlightResponseDto(f)).ToList();
        }

        private FlightResponseDto MapToFlightResponseDto(Flight f)
        {
            return new FlightResponseDto
            {
                FlightId = f.FlightId,
                FlightNumber = f.FlightNumber,
                AirlineName = f.Airline.AirlineName,
                AirlineCode = f.Airline.AirlineCode,
                DepartureAirport = $"{f.DepartureAirport.AirportName} ({f.DepartureAirport.AirportCode})",
                ArrivalAirport = $"{f.ArrivalAirport.AirportName} ({f.ArrivalAirport.AirportCode})",
                DepartureTime = f.DepartureTime,
                ArrivalTime = f.ArrivalTime,
                BasePrice = f.BasePrice,
                Status = f.Status,
                Gate = f.Gate,
                AvailableSeats = f.Seats.Count(s => s.IsAvailable ?? false)
            };
        }


        public async Task<PriceTrendDto> GetPriceTrendAsync(string route, int days = 30)
        {
            var routeParts = route.Split('-');
            if (routeParts.Length != 2)
                throw new ArgumentException("Invalid route format. Use 'FROM-TO' format.");

            var fromDate = DateTime.Now.Date;
            var toDate = fromDate.AddDays(days);

            var priceHistory = await _context.Flights
                .Where(f => f.DepartureAirport.AirportCode == routeParts[0]
                         && f.ArrivalAirport.AirportCode == routeParts[1]
                         && f.DepartureTime.Date >= fromDate
                         && f.DepartureTime.Date <= toDate)
                .GroupBy(f => f.DepartureTime.Date)
                .Select(g => new PricePointDto
                {
                    Date = g.Key,
                    MinPrice = g.Min(f => f.BasePrice),
                    MaxPrice = g.Max(f => f.BasePrice),
                    AvgPrice = g.Average(f => f.BasePrice),
                    FlightCount = g.Count()
                })
                .OrderBy(p => p.Date)
                .ToListAsync();

            return new PriceTrendDto
            {
                Route = route,
                PriceHistory = priceHistory,
                Recommendation = GeneratePriceRecommendation(priceHistory)
            };
        }

        private string GeneratePriceRecommendation(List<PricePointDto> priceHistory)
        {
            if (!priceHistory.Any())
                return "Không có dữ liệu để đưa ra khuyến nghị";

            var avgPrice = priceHistory.Average(p => p.AvgPrice);
            var minPrice = priceHistory.Min(p => p.MinPrice);
            var currentPrice = priceHistory.LastOrDefault()?.AvgPrice ?? 0;

            if (currentPrice <= minPrice * 1.1m)
                return "Đây là thời điểm tốt để đặt vé - giá đang ở mức thấp";
            else if (currentPrice >= avgPrice * 1.2m)
                return "Giá đang cao, nên chờ thêm vài ngày";
            else
                return "Giá ở mức trung bình, có thể đặt vé";
        }
    }
}
