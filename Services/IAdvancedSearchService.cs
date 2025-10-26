using FlightBooking.DTOs;

namespace FlightBooking.Services
{
    public interface IAdvancedSearchService
    {
        Task<FlightSearchResultDto> AdvancedSearchAsync(AdvancedFlightSearchDto searchDto);
        Task<List<string>> GetPopularDestinationsAsync(string fromAirport);
        Task<List<FlightResponseDto>> GetRecommendedFlightsAsync(int userId);
        Task<PriceTrendDto> GetPriceTrendAsync(string route, int days = 30);
    }
}
