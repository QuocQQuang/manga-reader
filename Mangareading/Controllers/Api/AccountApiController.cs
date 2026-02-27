using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Mangareading.Models.ViewModels.AccountApi;
using Mangareading.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic; // Added for List<Claim>
using Microsoft.AspNetCore.Http; // Added for StatusCodes

namespace Mangareading.Controllers.Api
{
    [Route("api/account")] // Changed route prefix for clarity
    [ApiController]
    [Authorize]
    // [ValidateAntiForgeryToken] // Apply globally for APIs requiring tokens - Let's add manually for now if needed, easier with JS fetch
    public class AccountApiController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IImgurService _imgurService;
        private readonly ILogger<AccountApiController> _logger;

        public AccountApiController(
            IUserService userService,
            IImgurService imgurService,
            ILogger<AccountApiController> logger)
        {
            _userService = userService;
            _imgurService = imgurService;
            _logger = logger;
        }

        // Helper to get current user ID
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim, out int userId))
            {  
                return userId;
            } 
            _logger.LogWarning("Could not retrieve User ID from claims in AccountApiController.");
            return null;
        }

        // PUT: api/account/email
        [HttpPut("email")] // Using PUT for updating existing resource data
        [ValidateAntiForgeryToken] // Keep AntiForgery for specific state-changing operations
        public async Task<IActionResult> UpdateEmail([FromBody] EmailUpdateModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            // Check for email conflict
            var existingUser = await _userService.GetUserByEmailAsync(model.NewEmail);
            if (existingUser != null && existingUser.UserId != userId.Value)
            {
                _logger.LogWarning("User {UserId} attempted to update email to {NewEmail}, but it already exists for user {ExistingUserId}.", userId.Value, model.NewEmail, existingUser.UserId);
                return Conflict(new { message = "Địa chỉ email này đã được sử dụng bởi một tài khoản khác." });
            }

            try
            {
                bool success = await _userService.UpdateEmailOnlyAsync(userId.Value, model.NewEmail); 

                if (success)
                {
                     _logger.LogInformation("User {UserId} successfully updated email to {NewEmail}.", userId.Value, model.NewEmail);

                    // --- Re-sign in to update claims --- 
                    var user = await _userService.GetUserByIdAsync(userId.Value); // Re-fetch user to get all claims data
                    if(user != null)
                    {
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                            new Claim(ClaimTypes.Name, user.Username),
                            new Claim(ClaimTypes.Email, user.Email), // Use updated email
                        };
                        if (user.IsAdmin) claims.Add(new Claim(ClaimTypes.Role, "Admin"));

                        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var principal = new ClaimsPrincipal(identity);
                        // Use current authentication properties if possible, especially IsPersistent
                        var props = (await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme))?.Properties ?? new AuthenticationProperties();
                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
                         _logger.LogInformation("User {UserId} re-signed in to update email claim.", userId.Value);
                    } else {
                         _logger.LogWarning("Could not re-fetch user {UserId} after email update for re-sign in.", userId.Value);
                    }
                    // --- End Re-sign in --- 

                    return Ok(new { message = "Cập nhật email thành công!" });
                } 
                else
                { 
                     _logger.LogError("Failed to update email for user {UserId} via API.", userId.Value);
                    // Consider if UserService provided a more specific reason for failure
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi khi cập nhật email." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception updating email for user {UserId} via API.", userId.Value);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi máy chủ khi cập nhật email." });
            }
        }

        // PUT: api/account/avatar
        [HttpPut("avatar")]
        [ValidateAntiForgeryToken] // Keep AntiForgery for specific state-changing operations
        public async Task<IActionResult> UpdateAvatar([FromBody] AvatarUpdateModel model)
        { 
             if (!ModelState.IsValid) // Validates the [Url] attribute
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            string? finalAvatarUrl = model.NewAvatarUrl; // Can be null/empty to clear

            // Resolve Imgur URL if provided
            if (!string.IsNullOrEmpty(finalAvatarUrl) && 
                (finalAvatarUrl.Contains("imgur.com", StringComparison.OrdinalIgnoreCase) || finalAvatarUrl.StartsWith("imgur.com")))
            {
                _logger.LogInformation("Attempting to resolve Imgur URL for avatar update (User ID {UserId}): {Url}", userId.Value, finalAvatarUrl);
                string? resolvedUrl = await _imgurService.ResolveDirectImageUrlAsync(finalAvatarUrl);
                if (resolvedUrl == null)
                {
                    _logger.LogWarning("Failed to resolve Imgur URL for avatar update (User ID {UserId}): {Url}", userId.Value, finalAvatarUrl);
                    return BadRequest(new { message = "Link Imgur không hợp lệ hoặc không tìm thấy ảnh." });
                }
                 _logger.LogInformation("Resolved Imgur URL for avatar update (User ID {UserId}) to: {ResolvedUrl}", userId.Value, resolvedUrl);
                finalAvatarUrl = resolvedUrl;
            }

            try
            {
                 bool success = await _userService.UpdateAvatarOnlyAsync(userId.Value, finalAvatarUrl);

                if (success)
                {
                    _logger.LogInformation("User {UserId} successfully updated avatar via API. New URL: {AvatarUrl}", userId.Value, string.IsNullOrEmpty(finalAvatarUrl) ? "Cleared" : finalAvatarUrl);
                    return Ok(new { message = "Cập nhật ảnh đại diện thành công!", newAvatarUrl = finalAvatarUrl }); // Return new URL for UI update
                }
                else
                { 
                     _logger.LogError("Failed to update avatar for user {UserId} via API.", userId.Value);
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi khi cập nhật ảnh đại diện." });
                }
            }
             catch (Exception ex)
            {
                _logger.LogError(ex, "Exception updating avatar for user {UserId} via API.", userId.Value);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi máy chủ khi cập nhật ảnh đại diện." });
            }
        }


        // POST: api/account/change-password
        [HttpPost("change-password")]
        [ValidateAntiForgeryToken] // Keep AntiForgery for specific state-changing operations
        public async Task<IActionResult> ChangePassword([FromBody] PasswordChangeModel model)
        { 
             if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

             try
            {
                var (success, errorMessage) = await _userService.ChangePasswordAsync(userId.Value, model.CurrentPassword, model.NewPassword);

                if (success)
                {
                    _logger.LogInformation("User {UserId} successfully changed password via API.", userId.Value);
                    return Ok(new { message = "Đổi mật khẩu thành công!" });
                }
                else
                { 
                    _logger.LogWarning("Password change failed for user {UserId} via API: {Error}", userId.Value, errorMessage);
                    // Return BadRequest with the specific error from the service
                    return BadRequest(new { message = errorMessage ?? "Đổi mật khẩu thất bại." });
                }
            }
             catch (Exception ex)
            {
                _logger.LogError(ex, "Exception changing password for user {UserId} via API.", userId.Value);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi máy chủ khi đổi mật khẩu." });
            }
        }
    }
} 