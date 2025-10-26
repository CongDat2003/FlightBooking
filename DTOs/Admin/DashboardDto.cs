namespace FlightBooking.DTOs.Admin
{
    public class DashboardStatsDto
    {
        public int TotalFlights { get; set; }
        public int TotalBookings { get; set; }
        public int TotalUsers { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TodayBookings { get; set; }
        public decimal TodayRevenue { get; set; }
        public List<RevenueByMonthDto> MonthlyRevenue { get; set; } = new List<RevenueByMonthDto>();
        public List<PopularRouteDto> PopularRoutes { get; set; } = new List<PopularRouteDto>();
        public List<BookingStatusStatsDto> BookingStats { get; set; } = new List<BookingStatusStatsDto>();
    }

    public class RevenueByMonthDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Revenue { get; set; }
        public int BookingCount { get; set; }
    }

    public class PopularRouteDto
    {
        public string Route { get; set; }
        public int BookingCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class BookingStatusStatsDto
    {
        public string Status { get; set; }
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }
}
