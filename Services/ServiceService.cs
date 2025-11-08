using FlightBooking.DTOs;
using FlightBooking.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlightBooking.Services
{
    public class ServiceService : IServiceService
    {
        private readonly FlightBookingContext _context;

        public ServiceService(FlightBookingContext context)
        {
            _context = context;
        }

        // Meal operations
        public async Task<List<MealDto>> GetAllMealsAsync()
        {
            return await _context.Meals
                .Where(m => m.IsActive)
                .Select(m => new MealDto
                {
                    MealId = m.MealId,
                    MealName = m.MealName,
                    Description = m.Description,
                    Price = m.Price,
                    MealType = m.MealType,
                    ImageUrl = m.ImageUrl,
                    ClassId = m.ClassId,
                    IsActive = m.IsActive
                })
                .ToListAsync();
        }

        public async Task<MealDto?> GetMealByIdAsync(int mealId)
        {
            var meal = await _context.Meals.FindAsync(mealId);
            if (meal == null) return null;

            return new MealDto
            {
                MealId = meal.MealId,
                MealName = meal.MealName,
                Description = meal.Description,
                Price = meal.Price,
                MealType = meal.MealType,
                ImageUrl = meal.ImageUrl,
                ClassId = meal.ClassId,
                IsActive = meal.IsActive
            };
        }

        public async Task<MealDto> CreateMealAsync(CreateMealDto createDto)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(createDto.MealName))
            {
                throw new ArgumentException("Meal name is required.");
            }

            if (createDto.Price < 0)
            {
                throw new ArgumentException("Price cannot be negative.");
            }

            var meal = new Meal
            {
                MealName = createDto.MealName.Trim(),
                Description = string.IsNullOrWhiteSpace(createDto.Description) ? null : createDto.Description.Trim(),
                Price = createDto.Price,
                MealType = string.IsNullOrWhiteSpace(createDto.MealType) ? null : createDto.MealType.Trim(),
                ImageUrl = string.IsNullOrWhiteSpace(createDto.ImageUrl) ? null : createDto.ImageUrl.Trim(),
                IsActive = true
                // CreatedAt will be set automatically by the model default value
            };

            try
            {
                _context.Meals.Add(meal);
                await _context.SaveChangesAsync();

                return new MealDto
                {
                    MealId = meal.MealId,
                    MealName = meal.MealName,
                    Description = meal.Description,
                    Price = meal.Price,
                    MealType = meal.MealType,
                    ImageUrl = meal.ImageUrl,
                    ClassId = meal.ClassId,
                    IsActive = meal.IsActive
                };
            }
            catch (Exception ex)
            {
                // Log the full exception for debugging
                throw new InvalidOperationException($"Failed to create meal: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateMealAsync(int mealId, UpdateMealDto updateDto)
        {
            var meal = await _context.Meals.FindAsync(mealId);
            if (meal == null) return false;

            if (updateDto.MealName != null) meal.MealName = updateDto.MealName;
            if (updateDto.Description != null) meal.Description = updateDto.Description;
            if (updateDto.Price.HasValue) meal.Price = updateDto.Price.Value;
            if (updateDto.MealType != null) meal.MealType = updateDto.MealType;
            if (updateDto.ImageUrl != null) meal.ImageUrl = updateDto.ImageUrl;
            if (updateDto.IsActive.HasValue) meal.IsActive = updateDto.IsActive.Value;
            meal.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteMealAsync(int mealId)
        {
            var meal = await _context.Meals.FindAsync(mealId);
            if (meal == null) return false;

            meal.IsActive = false;
            meal.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        // Luggage operations
        public async Task<List<LuggageDto>> GetAllLuggagesAsync()
        {
            return await _context.Luggages
                .Where(l => l.IsActive)
                .Select(l => new LuggageDto
                {
                    LuggageId = l.LuggageId,
                    LuggageName = l.LuggageName,
                    Description = l.Description,
                    Price = l.Price,
                    WeightLimit = l.WeightLimit,
                    LuggageType = l.LuggageType,
                    ImageUrl = l.ImageUrl,
                    IsActive = l.IsActive
                })
                .ToListAsync();
        }

        public async Task<LuggageDto?> GetLuggageByIdAsync(int luggageId)
        {
            var luggage = await _context.Luggages.FindAsync(luggageId);
            if (luggage == null) return null;

            return new LuggageDto
            {
                LuggageId = luggage.LuggageId,
                LuggageName = luggage.LuggageName,
                Description = luggage.Description,
                Price = luggage.Price,
                WeightLimit = luggage.WeightLimit,
                LuggageType = luggage.LuggageType,
                ImageUrl = luggage.ImageUrl,
                IsActive = luggage.IsActive
            };
        }

        public async Task<LuggageDto> CreateLuggageAsync(CreateLuggageDto createDto)
        {
            var luggage = new Luggage
            {
                LuggageName = createDto.LuggageName,
                Description = createDto.Description,
                Price = createDto.Price,
                WeightLimit = createDto.WeightLimit,
                LuggageType = createDto.LuggageType,
                ImageUrl = createDto.ImageUrl,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Luggages.Add(luggage);
            await _context.SaveChangesAsync();

            return new LuggageDto
            {
                LuggageId = luggage.LuggageId,
                LuggageName = luggage.LuggageName,
                Description = luggage.Description,
                Price = luggage.Price,
                WeightLimit = luggage.WeightLimit,
                LuggageType = luggage.LuggageType,
                ImageUrl = luggage.ImageUrl,
                IsActive = luggage.IsActive
            };
        }

        public async Task<bool> UpdateLuggageAsync(int luggageId, UpdateLuggageDto updateDto)
        {
            var luggage = await _context.Luggages.FindAsync(luggageId);
            if (luggage == null) return false;

            if (updateDto.LuggageName != null) luggage.LuggageName = updateDto.LuggageName;
            if (updateDto.Description != null) luggage.Description = updateDto.Description;
            if (updateDto.Price.HasValue) luggage.Price = updateDto.Price.Value;
            if (updateDto.WeightLimit.HasValue) luggage.WeightLimit = updateDto.WeightLimit.Value;
            if (updateDto.LuggageType != null) luggage.LuggageType = updateDto.LuggageType;
            if (updateDto.ImageUrl != null) luggage.ImageUrl = updateDto.ImageUrl;
            if (updateDto.IsActive.HasValue) luggage.IsActive = updateDto.IsActive.Value;
            luggage.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteLuggageAsync(int luggageId)
        {
            var luggage = await _context.Luggages.FindAsync(luggageId);
            if (luggage == null) return false;

            luggage.IsActive = false;
            luggage.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        // Insurance operations
        public async Task<List<InsuranceDto>> GetAllInsurancesAsync()
        {
            return await _context.Insurances
                .Where(i => i.IsActive)
                .Select(i => new InsuranceDto
                {
                    InsuranceId = i.InsuranceId,
                    InsuranceName = i.InsuranceName,
                    Description = i.Description,
                    Price = i.Price,
                    CoverageAmount = null,
                    InsuranceType = i.InsuranceType,
                    ImageUrl = i.ImageUrl,
                    IsActive = i.IsActive
                })
                .ToListAsync();
        }

        public async Task<InsuranceDto?> GetInsuranceByIdAsync(int insuranceId)
        {
            var insurance = await _context.Insurances.FindAsync(insuranceId);
            if (insurance == null) return null;

            return new InsuranceDto
            {
                InsuranceId = insurance.InsuranceId,
                InsuranceName = insurance.InsuranceName,
                Description = insurance.Description,
                Price = insurance.Price,
                CoverageAmount = null,
                InsuranceType = insurance.InsuranceType,
                ImageUrl = insurance.ImageUrl,
                IsActive = insurance.IsActive
            };
        }

        public async Task<InsuranceDto> CreateInsuranceAsync(CreateInsuranceDto createDto)
        {
            var insurance = new Insurance
            {
                InsuranceName = createDto.InsuranceName,
                Description = createDto.Description,
                Price = createDto.Price,
                CoverageAmount = null,
                InsuranceType = createDto.InsuranceType,
                ImageUrl = createDto.ImageUrl,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Insurances.Add(insurance);
            await _context.SaveChangesAsync();

            return new InsuranceDto
            {
                InsuranceId = insurance.InsuranceId,
                InsuranceName = insurance.InsuranceName,
                Description = insurance.Description,
                Price = insurance.Price,
                CoverageAmount = null,
                InsuranceType = insurance.InsuranceType,
                ImageUrl = insurance.ImageUrl,
                IsActive = insurance.IsActive
            };
        }

        public async Task<bool> UpdateInsuranceAsync(int insuranceId, UpdateInsuranceDto updateDto)
        {
            var insurance = await _context.Insurances.FindAsync(insuranceId);
            if (insurance == null) return false;

            if (updateDto.InsuranceName != null) insurance.InsuranceName = updateDto.InsuranceName;
            if (updateDto.Description != null) insurance.Description = updateDto.Description;
            if (updateDto.Price.HasValue) insurance.Price = updateDto.Price.Value;
            // CoverageAmount column does not exist in database, skipping
            if (updateDto.InsuranceType != null) insurance.InsuranceType = updateDto.InsuranceType;
            if (updateDto.ImageUrl != null) insurance.ImageUrl = updateDto.ImageUrl;
            if (updateDto.IsActive.HasValue) insurance.IsActive = updateDto.IsActive.Value;
            insurance.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteInsuranceAsync(int insuranceId)
        {
            var insurance = await _context.Insurances.FindAsync(insuranceId);
            if (insurance == null) return false;

            insurance.IsActive = false;
            insurance.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        // Booking service operations
        public async Task<List<BookingServiceDto>> GetBookingServicesAsync(int bookingId)
        {
            return await _context.BookingServices
                .Where(bs => bs.BookingId == bookingId)
                .Include(bs => bs.Meal)
                .Include(bs => bs.Luggage)
                .Include(bs => bs.Insurance)
                .Select(bs => new BookingServiceDto
                {
                    BookingServiceId = bs.BookingServiceId,
                    BookingId = bs.BookingId,
                    ServiceType = bs.ServiceType,
                    Meal = bs.Meal != null ? new MealDto
                    {
                        MealId = bs.Meal.MealId,
                        MealName = bs.Meal.MealName,
                        Description = bs.Meal.Description,
                        Price = bs.Meal.Price,
                        MealType = bs.Meal.MealType,
                        ImageUrl = bs.Meal.ImageUrl,
                        ClassId = bs.Meal.ClassId,
                        IsActive = bs.Meal.IsActive
                    } : null,
                    Luggage = bs.Luggage != null ? new LuggageDto
                    {
                        LuggageId = bs.Luggage.LuggageId,
                        LuggageName = bs.Luggage.LuggageName,
                        Description = bs.Luggage.Description,
                        Price = bs.Luggage.Price,
                        WeightLimit = bs.Luggage.WeightLimit,
                        LuggageType = bs.Luggage.LuggageType,
                        ImageUrl = bs.Luggage.ImageUrl,
                        IsActive = bs.Luggage.IsActive
                    } : null,
                    Insurance = bs.Insurance != null ? new InsuranceDto
                    {
                        InsuranceId = bs.Insurance.InsuranceId,
                        InsuranceName = bs.Insurance.InsuranceName,
                        Description = bs.Insurance.Description,
                        Price = bs.Insurance.Price,
                        CoverageAmount = null,
                        InsuranceType = bs.Insurance.InsuranceType,
                        ImageUrl = bs.Insurance.ImageUrl,
                        IsActive = bs.Insurance.IsActive
                    } : null,
                    Price = bs.Price,
                    Quantity = bs.Quantity
                })
                .ToListAsync();
        }

        public async Task<BookingServiceDto> AddServiceToBookingAsync(AddServiceToBookingDto addDto)
        {
            decimal price = 0;
            Meal? meal = null;
            Luggage? luggage = null;
            Insurance? insurance = null;

            switch (addDto.ServiceType.ToUpper())
            {
                case "MEAL":
                    if (!addDto.MealId.HasValue)
                        throw new ArgumentException("MealId is required for MEAL service type");
                    meal = await _context.Meals.FindAsync(addDto.MealId.Value);
                    if (meal == null || !meal.IsActive)
                        throw new InvalidOperationException("Meal not found or inactive");
                    price = meal.Price;
                    break;

                case "LUGGAGE":
                    if (!addDto.LuggageId.HasValue)
                        throw new ArgumentException("LuggageId is required for LUGGAGE service type");
                    luggage = await _context.Luggages.FindAsync(addDto.LuggageId.Value);
                    if (luggage == null || !luggage.IsActive)
                        throw new InvalidOperationException("Luggage not found or inactive");
                    price = luggage.Price;
                    break;

                case "INSURANCE":
                    if (!addDto.InsuranceId.HasValue)
                        throw new ArgumentException("InsuranceId is required for INSURANCE service type");
                    insurance = await _context.Insurances.FindAsync(addDto.InsuranceId.Value);
                    if (insurance == null || !insurance.IsActive)
                        throw new InvalidOperationException("Insurance not found or inactive");
                    price = insurance.Price;
                    break;

                default:
                    throw new ArgumentException("Invalid service type. Must be MEAL, LUGGAGE, or INSURANCE");
            }

            var booking = await _context.Bookings.FindAsync(addDto.BookingId);
            if (booking == null)
                throw new InvalidOperationException("Booking not found");

            var bookingService = new BookingService
            {
                BookingId = addDto.BookingId,
                ServiceType = addDto.ServiceType.ToUpper(),
                MealId = addDto.MealId,
                LuggageId = addDto.LuggageId,
                InsuranceId = addDto.InsuranceId,
                Price = price * addDto.Quantity,
                Quantity = addDto.Quantity,
                CreatedAt = DateTime.Now
            };

            _context.BookingServices.Add(bookingService);

            // Update booking total amount
            booking.TotalAmount += bookingService.Price;
            await _context.SaveChangesAsync();

            return new BookingServiceDto
            {
                BookingServiceId = bookingService.BookingServiceId,
                BookingId = bookingService.BookingId,
                ServiceType = bookingService.ServiceType,
                Meal = meal != null ? new MealDto
                {
                    MealId = meal.MealId,
                    MealName = meal.MealName,
                    Description = meal.Description,
                    Price = meal.Price,
                    MealType = meal.MealType,
                    ImageUrl = meal.ImageUrl,
                    ClassId = meal.ClassId,
                    IsActive = meal.IsActive
                } : null,
                Luggage = luggage != null ? new LuggageDto
                {
                    LuggageId = luggage.LuggageId,
                    LuggageName = luggage.LuggageName,
                    Description = luggage.Description,
                    Price = luggage.Price,
                    WeightLimit = luggage.WeightLimit,
                    LuggageType = luggage.LuggageType,
                    ImageUrl = luggage.ImageUrl,
                    IsActive = luggage.IsActive
                } : null,
                Insurance = insurance != null ? new InsuranceDto
                {
                    InsuranceId = insurance.InsuranceId,
                    InsuranceName = insurance.InsuranceName,
                    Description = insurance.Description,
                    Price = insurance.Price,
                    CoverageAmount = null,
                    InsuranceType = insurance.InsuranceType,
                    ImageUrl = insurance.ImageUrl,
                    IsActive = insurance.IsActive
                } : null,
                Price = bookingService.Price,
                Quantity = bookingService.Quantity
            };
        }

        public async Task<bool> RemoveServiceFromBookingAsync(int bookingServiceId)
        {
            var bookingService = await _context.BookingServices
                .Include(bs => bs.Booking)
                .FirstOrDefaultAsync(bs => bs.BookingServiceId == bookingServiceId);

            if (bookingService == null) return false;

            var booking = bookingService.Booking;
            booking.TotalAmount -= bookingService.Price;

            _context.BookingServices.Remove(bookingService);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> CalculateBookingServicesTotalAsync(int bookingId)
        {
            return await _context.BookingServices
                .Where(bs => bs.BookingId == bookingId)
                .SumAsync(bs => bs.Price);
        }
    }
}



