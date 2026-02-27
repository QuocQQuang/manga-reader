using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mangareading.Models;
using Mangareading.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Mangareading.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatisticsController : ControllerBase
    {
        private readonly StatisticsService _statisticsService;
        private readonly ILogger<StatisticsController> _logger;

        public StatisticsController(
            StatisticsService statisticsService,
            ILogger<StatisticsController> logger)
        {
            _statisticsService = statisticsService;
            _logger = logger;
        }

        // POST: api/Statistics/RecordView
        [HttpPost("RecordView")]
        public async Task<IActionResult> RecordView(int mangaId, int chapterId)
        {
            try
            {
                // Get user ID if logged in
                int? userId = null;
                if (User.Identity.IsAuthenticated)
                {
                    var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId" || c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
                    if (userIdClaim != null && !string.IsNullOrEmpty(userIdClaim.Value) && int.TryParse(userIdClaim.Value, out int parsedUserId))
                    {
                        userId = parsedUserId;
                    }
                }

                // Get IP address for anonymous tracking
                string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                await _statisticsService.RecordMangaViewAsync(mangaId, chapterId, userId, ipAddress);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording view for manga {MangaId}, chapter {ChapterId}", mangaId, chapterId);
                return StatusCode(500, "An error occurred while recording the view.");
            }
        }

        // GET: api/Statistics/MangaViews/{mangaId}
        [HttpGet("MangaViews/{mangaId}")]
        public async Task<ActionResult<int>> GetMangaViews(int mangaId)
        {
            try
            {
                var viewCount = await _statisticsService.GetTotalMangaViewsAsync(mangaId);
                return Ok(viewCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting view count for manga {MangaId}", mangaId);
                return StatusCode(500, "An error occurred while retrieving view count.");
            }
        }

        // GET: api/Statistics/MangaFavorites/{mangaId}
        [HttpGet("MangaFavorites/{mangaId}")]
        public async Task<ActionResult<int>> GetMangaFavorites(int mangaId)
        {
            try
            {
                var favoriteCount = await _statisticsService.GetTotalMangaFavoritesAsync(mangaId);
                return Ok(favoriteCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting favorite count for manga {MangaId}", mangaId);
                return StatusCode(500, "An error occurred while retrieving favorite count.");
            }
        }

        // GET: api/Statistics/MangaViewStats/{mangaId}
        [HttpGet("MangaViewStats/{mangaId}")]
        public async Task<ActionResult<Dictionary<DateTime, int>>> GetMangaViewStats(
            int mangaId, 
            string period = "day", 
            [FromQuery] DateTime? startDate = null, 
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var stats = await _statisticsService.GetViewStatisticsByPeriodAsync(mangaId, period, startDate, endDate);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting view statistics for manga {MangaId}", mangaId);
                return StatusCode(500, "An error occurred while retrieving view statistics.");
            }
        }

        // GET: api/Statistics/MangaFavoriteStats/{mangaId}
        [HttpGet("MangaFavoriteStats/{mangaId}")]
        public async Task<ActionResult<Dictionary<DateTime, int>>> GetMangaFavoriteStats(
            int mangaId, 
            string period = "day", 
            [FromQuery] DateTime? startDate = null, 
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var stats = await _statisticsService.GetFavoriteStatisticsByPeriodAsync(mangaId, period, startDate, endDate);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting favorite statistics for manga {MangaId}", mangaId);
                return StatusCode(500, "An error occurred while retrieving favorite statistics.");
            }
        }

        // GET: api/Statistics/ReadingHistory
        [HttpGet("ReadingHistory")]
        [Authorize]
        public async Task<ActionResult<List<ReadingHistory>>> GetReadingHistory([FromQuery] int? limit = 20)
        {
            try
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

                var history = await _statisticsService.GetUserReadingHistoryAsync(userId, limit);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reading history");
                return StatusCode(500, "An error occurred while retrieving reading history.");
            }
        }

        // GET: api/Statistics/TopViewedManga
        [HttpGet("TopViewedManga")]
        public async Task<ActionResult<List<object>>> GetTopViewedManga([FromQuery] int limit = 10)
        {
            try
            {
                var topMangas = await _statisticsService.GetTopViewedMangaAsync(limit);
                
                var result = topMangas.Select(t => new
                {
                    MangaId = t.Manga.MangaId,
                    Title = t.Manga.Title,
                    CoverUrl = t.Manga.CoverUrl,
                    ViewCount = t.ViewCount
                }).ToList();
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top viewed manga");
                return StatusCode(500, "An error occurred while retrieving top viewed manga.");
            }
        }
    }
}