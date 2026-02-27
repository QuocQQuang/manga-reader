using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Mangareading.Models;
using Mangareading.Repositories;
using System.Security.Claims;
using System.Linq;

namespace Mangareading.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MangaTrackingController : ControllerBase
    {
        private readonly IViewCountRepository _viewCountRepository;
        private readonly IFavoriteRepository _favoriteRepository;
        private readonly IReadingHistoryRepository _readingHistoryRepository;
        private readonly ILogger<MangaTrackingController> _logger;

        public MangaTrackingController(
            IViewCountRepository viewCountRepository,
            IFavoriteRepository favoriteRepository,
            IReadingHistoryRepository readingHistoryRepository,
            ILogger<MangaTrackingController> logger)
        {
            _viewCountRepository = viewCountRepository;
            _favoriteRepository = favoriteRepository;
            _readingHistoryRepository = readingHistoryRepository;
            _logger = logger;
        }

        // VIEWS ENDPOINTS

        [HttpPost("recordView")]
        public async Task<IActionResult> RecordView(int mangaId, int chapterId)
        {
            int? userId = null;
            
            // If user is authenticated, get their user ID
            if (User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.Claims.FirstOrDefault(c => 
                    c.Type == "UserId" || c.Type == ClaimTypes.NameIdentifier);
                    
                if (userIdClaim != null && !string.IsNullOrEmpty(userIdClaim.Value) && int.TryParse(userIdClaim.Value, out int id))
                {
                    userId = id;
                }
            }

            // Get client IP address 
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _viewCountRepository.AddViewAsync(mangaId, chapterId, userId, ipAddress);

            // Also add to reading history if the user is authenticated
            if (userId.HasValue)
            {
                await _readingHistoryRepository.AddToHistoryAsync(userId.Value, mangaId, chapterId);
            }

            return Ok(new { success = true, message = "View recorded successfully" });
        }

        [HttpPost("view/{mangaId}/{chapterId}")]
        public async Task<IActionResult> RecordViewByParams(int mangaId, int chapterId)
        {
            try
            {
                int? userId = null;
                
                // If user is authenticated, get their user ID
                if (User.Identity.IsAuthenticated)
                {
                    var userIdClaim = User.Claims.FirstOrDefault(c => 
                        c.Type == "UserId" || c.Type == ClaimTypes.NameIdentifier);
                        
                    if (userIdClaim != null && !string.IsNullOrEmpty(userIdClaim.Value) && int.TryParse(userIdClaim.Value, out int id))
                    {
                        userId = id;
                    }
                }

                // Get client IP address 
                string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                await _viewCountRepository.AddViewAsync(mangaId, chapterId, userId, ipAddress);

                // Also add to reading history if the user is authenticated
                if (userId.HasValue)
                {
                    await _readingHistoryRepository.AddToHistoryAsync(userId.Value, mangaId, chapterId);
                }
                
                _logger.LogDebug("Recorded view for manga {MangaId}, chapter {ChapterId} by user {UserId} from IP {IpAddress}", mangaId, chapterId, userId ?? 0, ipAddress);

                return Ok(new { success = true, message = "View recorded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording view for manga {MangaId}", mangaId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("viewCount/manga/{mangaId}")]
        public async Task<IActionResult> GetMangaViewCount(int mangaId)
        {
            var viewCount = await _viewCountRepository.GetMangaViewCountAsync(mangaId);
            return Ok(new { viewCount });
        }

        [HttpGet("viewCount/chapter/{chapterId}")]
        public async Task<IActionResult> GetChapterViewCount(int chapterId)
        {
            var viewCount = await _viewCountRepository.GetChapterViewCountAsync(chapterId);
            return Ok(new { viewCount });
        }

        // FAVORITES ENDPOINTS

        [Authorize]
        [HttpPost("favorite/{mangaId}")]
        public async Task<IActionResult> AddFavorite(int mangaId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _favoriteRepository.AddFavoriteAsync(userId, mangaId);
            return Ok();
        }

        [Authorize]
        [HttpDelete("favorite/{mangaId}")]
        public async Task<IActionResult> RemoveFavorite(int mangaId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _favoriteRepository.RemoveFavoriteAsync(userId, mangaId);
            return Ok();
        }

        [Authorize]
        [HttpGet("favorite/check/{mangaId}")]
        public async Task<IActionResult> CheckFavorite(int mangaId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            bool isFavorite = await _favoriteRepository.IsFavoriteAsync(userId, mangaId);
            return Ok(new { isFavorite });
        }

        [HttpGet("favorite/count/{mangaId}")]
        public async Task<IActionResult> GetFavoriteCount(int mangaId)
        {
            var favoriteCount = await _favoriteRepository.GetFavoriteCountAsync(mangaId);
            return Ok(new { favoriteCount });
        }

        [Authorize]
        [HttpGet("favorite/list")]
        public async Task<IActionResult> GetUserFavorites()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var favorites = await _favoriteRepository.GetUserFavoritesAsync(userId);
            return Ok(favorites);
        }

        // READING HISTORY ENDPOINTS

        [Authorize]
        [HttpGet("history")]
        public async Task<IActionResult> GetReadingHistory()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var history = await _readingHistoryRepository.GetUserHistoryAsync(userId);
            return Ok(history);
        }

        [Authorize]
        [HttpGet("history/manga/{mangaId}")]
        public async Task<IActionResult> GetLastReadChapter(int mangaId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var lastReadChapter = await _readingHistoryRepository.GetLastReadChapterAsync(userId, mangaId);
            return Ok(lastReadChapter);
        }

        [Authorize]
        [HttpDelete("history/clear")]
        public async Task<IActionResult> ClearReadingHistory()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _readingHistoryRepository.ClearHistoryAsync(userId);
            return Ok();
        }

        [Authorize]
        [HttpDelete("history/clear/{mangaId}")]
        public async Task<IActionResult> ClearMangaHistory(int mangaId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _readingHistoryRepository.ClearMangaHistoryAsync(userId, mangaId);
            return Ok();
        }
    }
}