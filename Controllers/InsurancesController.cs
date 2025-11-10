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
    public class InsurancesController : ControllerBase
    {
        private readonly IServiceService _serviceService;

        public InsurancesController(IServiceService serviceService)
        {
            _serviceService = serviceService;
        }

        [HttpGet]
        public async Task<ActionResult<List<InsuranceDto>>> GetAllInsurances()
        {
            try
            {
                var insurances = await _serviceService.GetAllInsurancesAsync();
                return Ok(insurances);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving insurances.", detail = ex.Message });
            }
        }

        [HttpGet("{insuranceId}")]
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
                return StatusCode(500, new { message = "An error occurred while retrieving insurance.", detail = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<InsuranceDto>> CreateInsurance([FromBody] CreateInsuranceDto createDto)
        {
            try
            {
                var insurance = await _serviceService.CreateInsuranceAsync(createDto);
                return CreatedAtAction(nameof(GetInsuranceById), new { insuranceId = insurance.InsuranceId }, insurance);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating insurance.", detail = ex.Message });
            }
        }

        [HttpPut("{insuranceId}")]
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
                return StatusCode(500, new { message = "An error occurred while updating insurance.", detail = ex.Message });
            }
        }

        [HttpDelete("{insuranceId}")]
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
                return StatusCode(500, new { message = "An error occurred while deleting insurance.", detail = ex.Message });
            }
        }
    }
}







































































