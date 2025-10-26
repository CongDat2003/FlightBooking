using FlightBooking.DTOs;
using FlightBooking.DTOs.User;
using FlightBooking.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace FlightBooking.Controllers.User
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserProfileDto>> Register([FromBody] RegisterUserDto registerDto)
        {
            try
            {
                var user = await _userService.RegisterAsync(registerDto);
                return CreatedAtAction(nameof(GetProfile), new { userId = user.UserId }, user);
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

        [HttpPost("login")]
        public async Task<ActionResult<UserProfileDto>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var user = await _userService.LoginAsync(loginDto);
                return Ok(user);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("profile/{userId}")]
        public async Task<ActionResult<UserProfileDto>> GetProfile(int userId)
        {
            try
            {
                var user = await _userService.GetProfileAsync(userId);
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

        [HttpPut("profile/{userId}")]
        public async Task<ActionResult<UserProfileDto>> UpdateProfile(int userId, [FromBody] UpdateProfileDto updateDto)
        {
            try
            {
                var user = await _userService.UpdateProfileAsync(userId, updateDto);
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

        [HttpPost("change-password/{userId}")]
        public async Task<ActionResult> ChangePassword(int userId, [FromBody] ChangePasswordDto passwordDto)
        {
            try
            {
                var result = await _userService.ChangePasswordAsync(userId, passwordDto);
                if (result)
                    return Ok(new { message = "Password changed successfully" });
                return BadRequest(new { message = "Failed to change password" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
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

        [HttpPost("delete-account/{userId}")]
        public async Task<ActionResult> DeleteAccount(int userId, [FromBody] DeleteAccountDto deleteDto)
        {
            try
            {
                var result = await _userService.DeleteAccountAsync(userId, deleteDto.Password);
                if (result)
                    return Ok(new { message = "Account deleted successfully" });
                return BadRequest(new { message = "Failed to delete account" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
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

        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            try
            {
                await _userService.SendPasswordResetOtpAsync(request.Email);
                return Ok(new { message = "Mã xác thực đã được gửi đến email của bạn." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("verify-otp")]
        public async Task<ActionResult> VerifyOtp([FromBody] VerifyOtpDto request)
        {
            try
            {
                var isValid = await _userService.VerifyPasswordResetOtpAsync(request.Email, request.OtpCode);
                if (!isValid)
                    return BadRequest(new { message = "Mã xác thực không hợp lệ hoặc đã hết hạn." });

                return Ok(new { message = "Mã xác thực hợp lệ." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            try
            {
                var result = await _userService.ResetPasswordAsync(request.Email, request.OtpCode, request.NewPassword);
                if (result)
                    return Ok(new { message = "Đổi mật khẩu thành công." });

                return BadRequest(new { message = "Không thể đổi mật khẩu." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class DeleteAccountDto
    {
        [Required]
        public string Password { get; set; } = null!;
    }
}
