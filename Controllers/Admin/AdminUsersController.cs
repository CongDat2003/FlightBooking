using FlightBooking.DTOs.Admin;
using FlightBooking.DTOs.User;
using FlightBooking.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlightBooking.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public UsersController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet]
        public async Task<ActionResult<List<AdminUserResponseDto>>> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var users = await _adminService.GetAllUsersAsync(page, pageSize);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<AdminUserResponseDto>> GetUserById(int userId)
        {
            try
            {
                var user = await _adminService.GetUserByIdAsync(userId);
                return Ok(user);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<AdminUserResponseDto>> CreateUser([FromBody] RegisterUserDto registerDto)
        {
            try
            {
                var user = await _adminService.CreateUserAsync(registerDto);
                return CreatedAtAction(nameof(GetUserById), new { userId = user.UserId }, user);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{userId}")]
        public async Task<ActionResult<AdminUserResponseDto>> UpdateUser(int userId, [FromBody] UpdateUserDto updateDto)
        {
            try
            {
                var user = await _adminService.UpdateUserAsync(userId, updateDto);
                return Ok(user);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{userId}/status")]
        public async Task<ActionResult<AdminUserResponseDto>> UpdateUserStatus(int userId, [FromBody] UpdateUserStatusDto statusDto)
        {
            try
            {
                var user = await _adminService.UpdateUserStatusAsync(userId, statusDto);
                return Ok(user);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{userId}")]
        public async Task<ActionResult> DeleteUser(int userId)
        {
            try
            {
                var success = await _adminService.DeleteUserAsync(userId);
                if (success)
                    return Ok(new { message = "User deleted successfully" });
                else
                    return BadRequest(new { message = "Failed to delete user" });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
