using FlightBooking.DTOs;
using FlightBooking.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightBooking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MealsController : ControllerBase
    {
        private readonly IServiceService _serviceService;

        public MealsController(IServiceService serviceService)
        {
            _serviceService = serviceService;
        }

        [HttpGet]
        public async Task<ActionResult<List<MealDto>>> GetAllMeals()
        {
            try
            {
                var meals = await _serviceService.GetAllMealsAsync();
                return Ok(meals);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving meals.", detail = ex.Message });
            }
        }

        [HttpGet("{mealId}")]
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
                return StatusCode(500, new { message = "An error occurred while retrieving meal.", detail = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<MealDto>> CreateMeal([FromBody] CreateMealDto createDto)
        {
            try
            {
                var meal = await _serviceService.CreateMealAsync(createDto);
                return CreatedAtAction(nameof(GetMealById), new { mealId = meal.MealId }, meal);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating meal.", detail = ex.Message });
            }
        }

        [HttpPut("{mealId}")]
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
                return StatusCode(500, new { message = "An error occurred while updating meal.", detail = ex.Message });
            }
        }

        [HttpDelete("{mealId}")]
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
                return StatusCode(500, new { message = "An error occurred while deleting meal.", detail = ex.Message });
            }
        }
    }
}


