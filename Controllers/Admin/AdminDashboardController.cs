using FlightBooking.DTOs.Admin;
using FlightBooking.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlightBooking.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public DashboardController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats()
        {
            try
            {
                var stats = await _adminService.GetDashboardStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("revenue/{year}")]
        public async Task<ActionResult<List<RevenueByMonthDto>>> GetRevenueReport(int year)
        {
            try
            {
                var revenue = await _adminService.GetRevenueReportAsync(year);
                return Ok(revenue);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("revenue/{startYear}/{endYear}")]
        public async Task<ActionResult<List<RevenueByMonthDto>>> GetRevenueReportRange(int startYear, int endYear)
        {
            try
            {
                if (startYear > endYear)
                    return BadRequest(new { message = "Start year must be less than or equal to end year" });

                if (endYear - startYear > 10)
                    return BadRequest(new { message = "Maximum range is 10 years" });

                var revenue = await _adminService.GetRevenueReportAsync(startYear, endYear);
                return Ok(revenue);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("revenue")]
        public async Task<ActionResult<List<RevenueByMonthDto>>> GetRevenueReportQuery(
       [FromQuery] int? year = null,
       [FromQuery] int? startYear = null,
       [FromQuery] int? endYear = null)
        {
            try
            {
                // Nếu có year thì dùng 1 năm
                if (year.HasValue)
                {
                    var singleYearRevenue = await _adminService.GetRevenueReportAsync(year.Value);
                    return Ok(singleYearRevenue);
                }

                // Nếu có startYear và endYear thì dùng khoảng năm
                if (startYear.HasValue && endYear.HasValue)
                {
                    if (startYear > endYear)
                        return BadRequest(new { message = "Start year must be less than or equal to end year" });

                    if (endYear - startYear > 10)
                        return BadRequest(new { message = "Maximum range is 10 years" });

                    var rangeRevenue = await _adminService.GetRevenueReportAsync(startYear.Value, endYear.Value);
                    return Ok(rangeRevenue);
                }

                // Mặc định lấy năm hiện tại
                var currentYear = DateTime.Now.Year;
                var defaultRevenue = await _adminService.GetRevenueReportAsync(currentYear);
                return Ok(defaultRevenue);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("popular-routes")]
        public async Task<ActionResult<List<PopularRouteDto>>> GetPopularRoutes([FromQuery] int topCount = 10)
        {
            try
            {
                var routes = await _adminService.GetPopularRoutesAsync(topCount);
                return Ok(routes);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
