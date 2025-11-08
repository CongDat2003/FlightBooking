using FlightBooking.DTOs;
using FlightBooking.Models;
using FlightBooking.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/Services")]
    public class AdminServicesController : ControllerBase
    {
        private readonly IServiceService _serviceService;
        private readonly FlightBookingContext _context;

        public AdminServicesController(IServiceService serviceService, FlightBookingContext context)
        {
            _serviceService = serviceService;
            _context = context;
        }

        // ==================== MEALS ====================
        [HttpGet("meals")]
        public async Task<ActionResult<List<MealDto>>> GetAllMeals()
        {
            try
            {
                var meals = await _serviceService.GetAllMealsAsync();
                return Ok(meals);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("meals/{mealId}")]
        public async Task<ActionResult<MealDto>> GetMealById(int mealId)
        {
            try
            {
                var meal = await _serviceService.GetMealByIdAsync(mealId);
                if (meal == null)
                    return NotFound(new { message = "Meal not found" });
                return Ok(meal);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("meals")]
        public async Task<ActionResult<MealDto>> CreateMeal([FromBody] CreateMealDto createDto)
        {
            try
            {
                var meal = await _serviceService.CreateMealAsync(createDto);
                return CreatedAtAction(nameof(GetMealById), new { mealId = meal.MealId }, meal);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("meals/{mealId}")]
        public async Task<ActionResult> UpdateMeal(int mealId, [FromBody] UpdateMealDto updateDto)
        {
            try
            {
                var result = await _serviceService.UpdateMealAsync(mealId, updateDto);
                if (!result)
                    return NotFound(new { message = "Meal not found" });
                return Ok(new { message = "Meal updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("meals/{mealId}")]
        public async Task<ActionResult> DeleteMeal(int mealId)
        {
            try
            {
                var result = await _serviceService.DeleteMealAsync(mealId);
                if (!result)
                    return NotFound(new { message = "Meal not found" });
                return Ok(new { message = "Meal deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==================== LUGGAGES ====================
        [HttpGet("luggages")]
        public async Task<ActionResult<List<LuggageDto>>> GetAllLuggages()
        {
            try
            {
                var luggages = await _serviceService.GetAllLuggagesAsync();
                return Ok(luggages);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("luggages/{luggageId}")]
        public async Task<ActionResult<LuggageDto>> GetLuggageById(int luggageId)
        {
            try
            {
                var luggage = await _serviceService.GetLuggageByIdAsync(luggageId);
                if (luggage == null)
                    return NotFound(new { message = "Luggage not found" });
                return Ok(luggage);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("luggages")]
        public async Task<ActionResult<LuggageDto>> CreateLuggage([FromBody] CreateLuggageDto createDto)
        {
            try
            {
                var luggage = await _serviceService.CreateLuggageAsync(createDto);
                return CreatedAtAction(nameof(GetLuggageById), new { luggageId = luggage.LuggageId }, luggage);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("luggages/{luggageId}")]
        public async Task<ActionResult> UpdateLuggage(int luggageId, [FromBody] UpdateLuggageDto updateDto)
        {
            try
            {
                var result = await _serviceService.UpdateLuggageAsync(luggageId, updateDto);
                if (!result)
                    return NotFound(new { message = "Luggage not found" });
                return Ok(new { message = "Luggage updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("luggages/{luggageId}")]
        public async Task<ActionResult> DeleteLuggage(int luggageId)
        {
            try
            {
                var result = await _serviceService.DeleteLuggageAsync(luggageId);
                if (!result)
                    return NotFound(new { message = "Luggage not found" });
                return Ok(new { message = "Luggage deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==================== INSURANCES ====================
        [HttpGet("insurances")]
        public async Task<ActionResult<List<InsuranceDto>>> GetAllInsurances()
        {
            try
            {
                var insurances = await _serviceService.GetAllInsurancesAsync();
                return Ok(insurances);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("insurances/{insuranceId}")]
        public async Task<ActionResult<InsuranceDto>> GetInsuranceById(int insuranceId)
        {
            try
            {
                var insurance = await _serviceService.GetInsuranceByIdAsync(insuranceId);
                if (insurance == null)
                    return NotFound(new { message = "Insurance not found" });
                return Ok(insurance);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("insurances")]
        public async Task<ActionResult<InsuranceDto>> CreateInsurance([FromBody] CreateInsuranceDto createDto)
        {
            try
            {
                var insurance = await _serviceService.CreateInsuranceAsync(createDto);
                return CreatedAtAction(nameof(GetInsuranceById), new { insuranceId = insurance.InsuranceId }, insurance);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("insurances/{insuranceId}")]
        public async Task<ActionResult> UpdateInsurance(int insuranceId, [FromBody] UpdateInsuranceDto updateDto)
        {
            try
            {
                var result = await _serviceService.UpdateInsuranceAsync(insuranceId, updateDto);
                if (!result)
                    return NotFound(new { message = "Insurance not found" });
                return Ok(new { message = "Insurance updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("insurances/{insuranceId}")]
        public async Task<ActionResult> DeleteInsurance(int insuranceId)
        {
            try
            {
                var result = await _serviceService.DeleteInsuranceAsync(insuranceId);
                if (!result)
                    return NotFound(new { message = "Insurance not found" });
                return Ok(new { message = "Insurance deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==================== STATISTICS ====================
        [HttpGet("stats/popular")]
        public async Task<ActionResult> GetPopularServices([FromQuery] int topCount = 10)
        {
            try
            {
                // Load all booking services with related entities
                var bookingServices = await _context.BookingServices
                    .Include(bs => bs.Meal)
                    .Include(bs => bs.Luggage)
                    .Include(bs => bs.Insurance)
                    .ToListAsync();

                // Popular Meals - case insensitive comparison
                var popularMeals = bookingServices
                    .Where(bs => bs.ServiceType != null && 
                            bs.ServiceType.ToUpper() == "MEAL" && 
                            bs.MealId != null && 
                            bs.Meal != null)
                    .GroupBy(bs => new { bs.MealId, bs.Meal.MealName })
                    .Select(g => new
                    {
                        ServiceId = g.Key.MealId ?? 0,
                        ServiceName = g.Key.MealName ?? "Unknown",
                        ServiceType = "MEAL",
                        TotalBookings = g.Count(),
                        TotalQuantity = g.Sum(bs => bs.Quantity),
                        TotalRevenue = g.Sum(bs => bs.Price * bs.Quantity)
                    })
                    .OrderByDescending(x => x.TotalBookings)
                    .Take(topCount)
                    .ToList();

                // Popular Luggages - case insensitive comparison
                var popularLuggages = bookingServices
                    .Where(bs => bs.ServiceType != null && 
                            bs.ServiceType.ToUpper() == "LUGGAGE" && 
                            bs.LuggageId != null && 
                            bs.Luggage != null)
                    .GroupBy(bs => new { bs.LuggageId, bs.Luggage.LuggageName })
                    .Select(g => new
                    {
                        ServiceId = g.Key.LuggageId ?? 0,
                        ServiceName = g.Key.LuggageName ?? "Unknown",
                        ServiceType = "LUGGAGE",
                        TotalBookings = g.Count(),
                        TotalQuantity = g.Sum(bs => bs.Quantity),
                        TotalRevenue = g.Sum(bs => bs.Price * bs.Quantity)
                    })
                    .OrderByDescending(x => x.TotalBookings)
                    .Take(topCount)
                    .ToList();

                // Popular Insurances - case insensitive comparison
                var popularInsurances = bookingServices
                    .Where(bs => bs.ServiceType != null && 
                            bs.ServiceType.ToUpper() == "INSURANCE" && 
                            bs.InsuranceId != null && 
                            bs.Insurance != null)
                    .GroupBy(bs => new { bs.InsuranceId, bs.Insurance.InsuranceName })
                    .Select(g => new
                    {
                        ServiceId = g.Key.InsuranceId ?? 0,
                        ServiceName = g.Key.InsuranceName ?? "Unknown",
                        ServiceType = "INSURANCE",
                        TotalBookings = g.Count(),
                        TotalQuantity = g.Sum(bs => bs.Quantity),
                        TotalRevenue = g.Sum(bs => bs.Price * bs.Quantity)
                    })
                    .OrderByDescending(x => x.TotalBookings)
                    .Take(topCount)
                    .ToList();

                return Ok(new
                {
                    popularMeals,
                    popularLuggages,
                    popularInsurances
                });
            }
            catch (Exception ex)
            {
                // If table doesn't exist, return empty results
                if (ex.Message.Contains("Invalid object name") || 
                    ex.Message.Contains("booking_services") ||
                    ex.Message.Contains("Cannot find table"))
                {
                    return Ok(new
                    {
                        popularMeals = new List<object>(),
                        popularLuggages = new List<object>(),
                        popularInsurances = new List<object>()
                    });
                }
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

