using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Linq;
using Mangareading.Repositories;
using Mangareading.Services;
using Mangareading.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace Mangareading.Controllers
{
    public class UserController : Controller
    {
        private readonly IReadingHistoryRepository _readingHistoryRepository;
        private readonly IFavoriteRepository _favoriteRepository;
        private readonly IMangaStatisticsService _statisticsService;
        private readonly ILogger<UserController> _logger;
        private readonly YourDbContext _dbContext;

        public UserController(
            IReadingHistoryRepository readingHistoryRepository,
            IFavoriteRepository favoriteRepository,
            IMangaStatisticsService statisticsService,
            YourDbContext dbContext,
            ILogger<UserController> logger)
        {
            _readingHistoryRepository = readingHistoryRepository;
            _favoriteRepository = favoriteRepository;
            _statisticsService = statisticsService;
            _dbContext = dbContext;
            _logger = logger;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            // Default user page that can provide an overview
            return View();
        }

        [Authorize]
        public async Task<IActionResult> ReadingHistory()
        {
            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId" || c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
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
                _logger.LogError(ex, "Error retrieving reading history");
                return View(new System.Collections.Generic.List<Mangareading.Models.ReadingHistory>());
            }
        }

        [Authorize]
        public async Task<IActionResult> Favorites()
        {
            try
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId" || c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
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
                _logger.LogError(ex, "Error retrieving user favorites");
                return View(new System.Collections.Generic.List<Mangareading.Models.Manga>());
            }
        }
        
        [Authorize(Roles = "Admin")]
        [HttpGet("User/ManageManga/{mangaId}")]
        public async Task<IActionResult> ManageManga(int mangaId)
        {
            try
            {
                var manga = await _dbContext.Mangas
                    .Include(m => m.Chapters)
                    .FirstOrDefaultAsync(m => m.MangaId == mangaId);
                    
                if (manga == null)
                {
                    return NotFound();
                }
                
                return View(manga);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving manga with ID {mangaId} for management");
                return RedirectToAction("Dashboard", "Admin");
            }
        }

        // API endpoint to get user information by ID
        [HttpGet("api/User/{userId}")]
        [AllowAnonymous] // Allow anonymous access for comment display
        public async Task<IActionResult> GetUserById(int userId)
        {
            try
            {
                var user = await _dbContext.Users
                    .Where(u => u.UserId == userId)
                    .Select(u => new { u.UserId, u.Username, u.AvatarUrl }) // Only return necessary fields
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound();
                }

                return Json(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user with ID {userId}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SaveThemePreference([FromBody] ThemePreferenceDto model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Theme) || (model.Theme != "light" && model.Theme != "dark"))
                {
                    return BadRequest(new { success = false, message = "Invalid theme preference" });
                }

                // Get current user
                var username = User.Identity.Name;
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                // Update theme preference
                user.ThemePreference = model.Theme;
                
                // Also save in session for quick access
                HttpContext.Session.SetString("UserTheme", model.Theme);

                // Save to database
                await _dbContext.SaveChangesAsync();

                return Ok(new { success = true, message = "Theme preference saved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving theme preference");
                return StatusCode(500, new { success = false, message = "An error occurred while saving theme preference" });
            }
        }
    }
}