using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mangareading.Models;
using Mangareading.Repositories;
using Mangareading.Services;
using Microsoft.AspNetCore.Http;

namespace Mangareading.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MangaStatisticsController : ControllerBase
    {
        private readonly IViewCountRepository _viewCountRepository;
        private readonly IFavoriteRepository _favoriteRepository;
        private readonly IReadingHistoryRepository _readingHistoryRepository;
        private readonly IMangaStatisticsService _statisticsService;

        public MangaStatisticsController(
            IViewCountRepository viewCountRepository,
            IFavoriteRepository favoriteRepository,
            IReadingHistoryRepository readingHistoryRepository,
            IMangaStatisticsService statisticsService)
        {
            _viewCountRepository = viewCountRepository;
            _favoriteRepository = favoriteRepository;
            _readingHistoryRepository = readingHistoryRepository;
            _statisticsService = statisticsService;
        }

        [HttpPost("view/{mangaId}/{chapterId}")]
        public async Task<IActionResult> RecordView(int mangaId, int chapterId)
        {
            try
            {
                // Get user ID if user is logged in
                int? userId = null;
                if (User.Identity.IsAuthenticated)
                {
                    var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId" || c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
                    if (userIdClaim != null && !string.IsNullOrEmpty(userIdClaim.Value) && int.TryParse(userIdClaim.Value, out int parsedUserId))
                    {
                        userId = parsedUserId;
                    }
                }

                // Get client IP address
                string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                // Record the view
                await _viewCountRepository.AddViewAsync(mangaId, chapterId, userId, ipAddress);

                // If user is logged in, update reading history
                if (userId.HasValue)
                {
                    await _readingHistoryRepository.AddToHistoryAsync(userId.Value, mangaId, chapterId);
                }

                return Ok(new { success = true, message = "View recorded successfully" });
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error recording view: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("views/{mangaId}")]
        public async Task<IActionResult> GetMangaViews(int mangaId)
        {
            var totalViews = await _viewCountRepository.GetMangaViewCountAsync(mangaId);
            return Ok(new { TotalViews = totalViews });
        }

        [HttpGet("chapter-views/{chapterId}")]
        public async Task<IActionResult> GetChapterViews(int chapterId)
        {
            var views = await _viewCountRepository.GetChapterViewCountAsync(chapterId);
            return Ok(new { Views = views });
        }

        [HttpGet("statistics/{mangaId}")]
        public async Task<IActionResult> GetMangaStatistics(
            int mangaId, 
            [FromQuery] DateTime? startDate = null, 
            [FromQuery] DateTime? endDate = null)
        {
            var statistics = await _statisticsService.GetMangaStatisticsAsync(mangaId, startDate, endDate);
            return Ok(statistics);
        }

        [HttpGet("daily-views/{mangaId}")]
        public async Task<IActionResult> GetDailyViews(
            int mangaId, 
            [FromQuery] DateTime startDate, 
            [FromQuery] DateTime endDate)
        {
            var dailyViews = await _statisticsService.GetDailyViewsAsync(mangaId, startDate, endDate);
            return Ok(dailyViews);
        }

        [HttpGet("monthly-views/{mangaId}")]
        public async Task<IActionResult> GetMonthlyViews(int mangaId, [FromQuery] int year)
        {
            var monthlyViews = await _statisticsService.GetMonthlyViewsAsync(mangaId, year);
            return Ok(monthlyViews);
        }

        [HttpGet("yearly-views/{mangaId}")]
        public async Task<IActionResult> GetYearlyViews(int mangaId)
        {
            var yearlyViews = await _statisticsService.GetYearlyViewsAsync(mangaId);
            return Ok(yearlyViews);
        }

        [Authorize]
        [HttpPost("favorite/{mangaId}")]
        public async Task<IActionResult> ToggleFavorite(int mangaId)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId" || c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
            {
                return Unauthorized();
            }
            
            if (!int.TryParse(userIdClaim.Value, out int userId))
            {
                return BadRequest("Invalid user ID format");
            }
            
            // Check if manga is already favorited
            bool isFavorite = await _favoriteRepository.IsFavoriteAsync(userId, mangaId);
            
            if (isFavorite)
            {
                // Remove from favorites
                await _favoriteRepository.RemoveFavoriteAsync(userId, mangaId);
                return Ok(new { IsFavorite = false });
            }
            else
            {
                // Add to favorites
                await _favoriteRepository.AddFavoriteAsync(userId, mangaId);
                return Ok(new { IsFavorite = true });
            }
        }

        [Authorize]
        [HttpGet("is-favorite/{mangaId}")]
        public async Task<IActionResult> IsFavorite(int mangaId)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId" || c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
            {
                return Unauthorized();
            }
            
            if (!int.TryParse(userIdClaim.Value, out int userId))
            {
                return BadRequest("Invalid user ID format");
            }
            
            bool isFavorite = await _favoriteRepository.IsFavoriteAsync(userId, mangaId);
            return Ok(new { IsFavorite = isFavorite });
        }

        [HttpGet("favorite-count/{mangaId}")]
        public async Task<IActionResult> GetFavoriteCount(int mangaId)
        {
            int favoriteCount = await _favoriteRepository.GetFavoriteCountAsync(mangaId);
            return Ok(new { FavoriteCount = favoriteCount });
        }
    }
}