using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Mangareading.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // Add this for session access
using Microsoft.AspNetCore.Authorization;
using Mangareading.Services.Interfaces;
using Mangareading.Repositories;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding; // Added for ModelStateDictionary extension

namespace Mangareading.Controllers
{
    [Authorize] // Require authentication for all actions in this controller
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly IReadingHistoryRepository _readingHistoryRepository;
        private readonly IFavoriteRepository _favoriteRepository;
        private readonly ILogger<AccountController> _logger;
        private readonly IImgurService _imgurService;

        public AccountController(
            IUserService userService,
            IReadingHistoryRepository readingHistoryRepository = null,
            IFavoriteRepository favoriteRepository = null,
            ILogger<AccountController> logger = null,
            IImgurService imgurService = null)
        {
            _userService = userService;
            _readingHistoryRepository = readingHistoryRepository;
            _favoriteRepository = favoriteRepository;
            _logger = logger;
            _imgurService = imgurService;
        }

        // GET: /Account/Register
        [AllowAnonymous] // Allow anonymous access to Register page
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous] // Allow anonymous access to Register action
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem tên đăng nhập đã tồn tại chưa
                if (await _userService.IsUsernameExistsAsync(model.Username))
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
                    return View(model);
                }

                // Kiểm tra xem email đã tồn tại chưa
                if (await _userService.IsEmailExistsAsync(model.Email))
                {
                    ModelState.AddModelError("Email", "Email đã tồn tại");
                    return View(model);
                }

                // Đăng ký người dùng
                User user = await _userService.RegisterUserAsync(model); // Assuming your service now returns the User object
                if (user != null)
                {
                    // Create claims
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Email, user.Email),
                    };
                    
                    // Thêm claim role Admin nếu user có quyền admin
                    if (user.IsAdmin)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                    }

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    // Store something in session after successful registration/login
                    HttpContext.Session.SetString("Username", user.Username); // Example: store username in session

                    TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Chào mừng " + user.Username + "!";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Đăng ký tài khoản không thành công. Vui lòng thử lại sau.");
                }
            }

            // If there are errors, redisplay the registration form
            return View(model);
        }

        // GET: /Account/Login
        [AllowAnonymous] // Allow anonymous access to Login page
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous] // Allow anonymous access to Login action
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                User user = await _userService.AuthenticateUserAsync(model.Username, model.Password); // Implement this in your service

                if (user != null)
                {
                    // Create claims
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Email, user.Email),
                    };
                    
                    // Thêm claim role Admin nếu user có quyền admin
                    if (user.IsAdmin)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                    }

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    // Store something in session
                    HttpContext.Session.SetString("Username", user.Username);

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    // Kiểm tra xem người dùng có tồn tại nhưng bị vô hiệu hóa không
                    var existingUser = await _userService.GetUserByUsernameAsync(model.Username);
                    if (existingUser != null && !existingUser.IsActive)
                    {
                        ModelState.AddModelError("", "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Đăng nhập không thành công. Vui lòng kiểm tra tên đăng nhập và mật khẩu.");
                    }
                    return View(model);
                }
            }

            return View(model);
        }

        // GET: /Account/Logout
        // [Authorize] is implicitly applied due to controller-level attribute
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear(); // Clear the session
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Profile
        // [Authorize] is implicitly applied
        public async Task<IActionResult> Profile()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _userService.GetUserByIdAsync(userId.Value);
            if (user == null)
            {
                _logger.LogWarning($"Profile requested for non-existent user ID: {userId.Value}");
                // Redirect to home or show an error view
                TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng.";
                return RedirectToAction("Index", "Home"); 
            }

            var viewModel = new ProfileViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt,
                IsAdmin = user.IsAdmin,
                IsActive = user.IsActive,
                ThemePreference = user.ThemePreference
            };

            // Pass the combined model to the view
            ViewData["UpdateModel"] = new ProfileUpdateViewModel { Email = user.Email, AvatarUrl = user.AvatarUrl };
            return View(viewModel);
        }

        // POST: /Account/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileUpdateViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            // Only validate necessary fields if password fields are empty
             if (string.IsNullOrEmpty(model.CurrentPassword) && string.IsNullOrEmpty(model.NewPassword) && string.IsNullOrEmpty(model.ConfirmPassword))
            {
                ModelState.Remove(nameof(model.CurrentPassword));
                ModelState.Remove(nameof(model.NewPassword));
                ModelState.Remove(nameof(model.ConfirmPassword));
            }

            // Check specific fields
             if (!(ModelState[nameof(model.Email)]?.ValidationState == ModelValidationState.Valid) || 
                 !(ModelState[nameof(model.AvatarUrl)]?.ValidationState == ModelValidationState.Valid && (string.IsNullOrEmpty(model.AvatarUrl) || Uri.TryCreate(model.AvatarUrl, UriKind.Absolute, out _)))) // Basic URL format validation
            {
                TempData["ErrorMessage"] = "Dữ liệu cập nhật hồ sơ không hợp lệ (Email hoặc URL ảnh đại diện).";
                return await ReloadProfileViewWithError(userId.Value, model);
            }
            
            string finalAvatarUrl = model.AvatarUrl; // Default to user input

            // --- Imgur Link Resolution --- 
            if (!string.IsNullOrEmpty(model.AvatarUrl) && 
                (model.AvatarUrl.Contains("imgur.com", StringComparison.OrdinalIgnoreCase) || model.AvatarUrl.StartsWith("imgur.com")))
            {
                _logger.LogInformation("Attempting to resolve Imgur URL for User ID {UserId}: {Url}", userId.Value, model.AvatarUrl);
                string? resolvedUrl = await _imgurService.ResolveDirectImageUrlAsync(model.AvatarUrl);

                if (resolvedUrl == null)
                {
                    _logger.LogWarning("Failed to resolve Imgur URL for User ID {UserId}: {Url}", userId.Value, model.AvatarUrl);
                    TempData["ErrorMessage"] = "Link Imgur không hợp lệ hoặc không tìm thấy ảnh. Vui lòng kiểm tra lại link hoặc sử dụng link ảnh trực tiếp.";
                    return await ReloadProfileViewWithError(userId.Value, model);
                }
                 _logger.LogInformation("Successfully resolved Imgur URL for User ID {UserId} to: {ResolvedUrl}", userId.Value, resolvedUrl);
                finalAvatarUrl = resolvedUrl;
            }
             // --- End Imgur Link Resolution --- 

            // Check for email conflict after potential Imgur resolution
            var existingUser = await _userService.GetUserByEmailAsync(model.Email);
            if (existingUser != null && existingUser.UserId != userId.Value)
            {
                TempData["ErrorMessage"] = "Email này đã được sử dụng bởi một tài khoản khác.";
                 return await ReloadProfileViewWithError(userId.Value, model); // Use helper
            }

            bool success = await _userService.UpdateProfileAsync(userId.Value, model.Email, finalAvatarUrl); // Use finalAvatarUrl

            if (success)
            {
                TempData["SuccessMessage"] = "Cập nhật thông tin hồ sơ thành công!";
                // Cập nhật lại Email trong Claims nếu thay đổi
                var currentEmailClaim = User.FindFirst(ClaimTypes.Email);
                if(currentEmailClaim != null && currentEmailClaim.Value != model.Email)
                {
                    // Re-sign in user to update claims immediately
                    var user = await _userService.GetUserByIdAsync(userId.Value);
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Email, user.Email), // Use updated email
                    };
                    if (user.IsAdmin) claims.Add(new Claim(ClaimTypes.Role, "Admin"));

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties { IsPersistent = true });
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Cập nhật hồ sơ thất bại. Vui lòng thử lại.";
            }

            return RedirectToAction("Profile");
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ProfileUpdateViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            // Validate password fields specifically for this action
            if (string.IsNullOrEmpty(model.CurrentPassword) || string.IsNullOrEmpty(model.NewPassword) || string.IsNullOrEmpty(model.ConfirmPassword))
            {
                 TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin mật khẩu.";
                 return RedirectToAction("Profile");
            }
             if (model.NewPassword != model.ConfirmPassword)
            {
                TempData["ErrorMessage"] = "Mật khẩu mới và mật khẩu xác nhận không khớp.";
                 return RedirectToAction("Profile");
            }
              if (!(ModelState[nameof(model.CurrentPassword)]?.ValidationState == ModelValidationState.Valid) || 
                !(ModelState[nameof(model.NewPassword)]?.ValidationState == ModelValidationState.Valid) || 
                !(ModelState[nameof(model.ConfirmPassword)]?.ValidationState == ModelValidationState.Valid))
            {
                TempData["ErrorMessage"] = "Thông tin mật khẩu không hợp lệ.";
                 // Load profile data again before returning the view
                 return await ReloadProfileViewWithError(userId.Value, model); // Use helper
            }

            var (success, errorMessage) = await _userService.ChangePasswordAsync(userId.Value, model.CurrentPassword, model.NewPassword);

            if (success)
            {
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = errorMessage ?? "Đổi mật khẩu thất bại. Vui lòng thử lại.";
            }

            return RedirectToAction("Profile");
        }
        
        // [Authorize] is implicitly applied
        public async Task<IActionResult> ReadingHistory()
        {
            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value) || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    // Xử lý lỗi hoặc trả về Unauthorized/Redirect
                    return Unauthorized();
                }
                var history = await _readingHistoryRepository.GetUserHistoryAsync(userId);
                return View(history);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving reading history");
                return View(new List<ReadingHistory>());
            }
        }
        
        // [Authorize] is implicitly applied
        public async Task<IActionResult> Favorites()
        {
            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value) || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    // Xử lý lỗi hoặc trả về Unauthorized/Redirect
                    return Unauthorized();
                }
                var favorites = await _favoriteRepository.GetUserFavoritesAsync(userId);
                return View(favorites);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving user favorites");
                return View(new List<Manga>());
            }
        }

        // Helper method to get current user ID
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            _logger.LogWarning("Could not retrieve User ID from claims.");
            return null;
        }

        // Helper method to reload profile view with errors
        private async Task<IActionResult> ReloadProfileViewWithError(int userId, ProfileUpdateViewModel updateModel)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return RedirectToAction("Index", "Home"); // Or handle error appropriately

            var viewModel = new ProfileViewModel 
            { 
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email, 
                AvatarUrl = user.AvatarUrl,
                CreatedAt = user.CreatedAt,
                IsAdmin = user.IsAdmin,
                IsActive = user.IsActive,
                ThemePreference = user.ThemePreference
            };
            ViewData["UpdateModel"] = updateModel; // Keep user input for the forms
            return View("Profile", viewModel);
        }
    }
}