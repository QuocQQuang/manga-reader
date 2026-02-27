using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mangareading.Services.Interfaces;

namespace Mangareading.Controllers
{
    [ApiController]
    [Route("api/time-stats")]
    public class TimeBasedStatsController : ControllerBase
    {
        private readonly IStatsService _statsService;
        private readonly ILogger<TimeBasedStatsController> _logger;

        public TimeBasedStatsController(
            IStatsService statsService,
            ILogger<TimeBasedStatsController> logger)
        {
            _statsService = statsService;
            _logger = logger;
        }

        [HttpGet("basic")]
        public async Task<IActionResult> GetBasicStats()
        {
            try
            {
                var stats = await _statsService.GetBasicStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving basic statistics");
                return StatusCode(500, "An error occurred while retrieving statistics");
            }
        }

        [HttpGet("genre-distribution")]
        public async Task<IActionResult> GetGenreDistribution()
        {
            try
            {
                var distribution = await _statsService.GetGenreDistributionAsync();
                
                // Transform data for chart.js format
                var labels = new List<string>();
                var data = new List<int>();
                
                foreach (var entry in distribution)
                {
                    labels.Add(entry.Key);
                    data.Add(entry.Value);
                }
                
                return Ok(new { labels, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving genre distribution");
                return StatusCode(500, "An error occurred while retrieving genre distribution");
            }
        }

        [HttpGet("views/{period}")]
        public async Task<IActionResult> GetViewsByPeriod(string period)
        {
            try
            {
                if (string.IsNullOrEmpty(period))
                {
                    period = "month"; // Default to month if not specified
                }
                
                var validPeriods = new[] { "day", "week", "month", "year" };
                if (!Array.Exists(validPeriods, p => p == period.ToLower()))
                {
                    return BadRequest($"Invalid period. Valid values are: {string.Join(", ", validPeriods)}");
                }
                
                var viewsData = await _statsService.GetViewsDataByPeriodAsync(period);
                return Ok(viewsData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving views by period");
                return StatusCode(500, "An error occurred while retrieving views data");
            }
        }

        [HttpGet("user-growth/{period}")]
        public async Task<IActionResult> GetUserGrowthByPeriod(string period)
        {
            try
            {
                if (string.IsNullOrEmpty(period))
                {
                    period = "month"; // Default to month if not specified
                }
                
                var validPeriods = new[] { "day", "week", "month", "year" };
                if (!Array.Exists(validPeriods, p => p == period.ToLower()))
                {
                    return BadRequest($"Invalid period. Valid values are: {string.Join(", ", validPeriods)}");
                }
                
                var userGrowthData = await _statsService.GetUserGrowthByPeriodAsync(period);
                return Ok(userGrowthData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user growth by period");
                return StatusCode(500, "An error occurred while retrieving user growth data");
            }
        }

        [HttpGet("range")]
        public async Task<IActionResult> GetStatsByRange([FromQuery] DateTime? start, [FromQuery] DateTime? end)
        {
            try
            {
                DateTime startDate = start ?? DateTime.UtcNow.AddDays(-30);
                DateTime endDate = end ?? DateTime.UtcNow;
                
                var stats = await _statsService.GetStatsByTimeRangeAsync(startDate, endDate);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving statistics by range");
                return StatusCode(500, "An error occurred while retrieving statistics by range");
            }
        }

        [HttpGet("top-manga")]
        public async Task<IActionResult> GetTopManga([FromQuery] DateTime? start, [FromQuery] DateTime? end, [FromQuery] int? count)
        {
            try
            {
                DateTime startDate = start ?? DateTime.UtcNow.AddDays(-30);
                DateTime endDate = end ?? DateTime.UtcNow;
                int mangaCount = count ?? 10;
                
                var topManga = await _statsService.GetTopMangaByTimeRangeAsync(startDate, endDate, mangaCount);
                
                // Transform to simplified format for frontend
                var result = new List<object>();
                foreach (var manga in topManga)
                {
                    result.Add(new
                    {
                        manga.MangaId,
                        manga.Title,
                        manga.CoverUrl,
                        manga.ViewCount
                    });
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving top manga by time range");
                return StatusCode(500, "An error occurred while retrieving top manga data");
            }
        }
    }
}
