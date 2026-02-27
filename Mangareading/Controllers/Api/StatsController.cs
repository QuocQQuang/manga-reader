using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mangareading.Models;
using Mangareading.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Mangareading.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatsController : ControllerBase
    {
        private readonly IStatsService _statsService;
        private readonly ILogger<StatsController> _logger;
        private readonly YourDbContext _dbContext;

        public StatsController(
            IStatsService statsService,
            YourDbContext dbContext, 
            ILogger<StatsController> logger)
        {
            _statsService = statsService;
            _dbContext = dbContext;
            _logger = logger;
        }

        // GET: api/stats/basic
        [HttpGet("basic")]
        public async Task<ActionResult<object>> GetBasicStats()
        {
            try
            {
                var stats = await _statsService.GetBasicStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving basic statistics");
                return StatusCode(500, "An error occurred while retrieving basic statistics");
            }
        }

        [HttpGet("update")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UpdateStats()
        {
            try
            {
                await _statsService.UpdateStatsAsync();
                return Ok(new { message = "Statistics updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating statistics");
                return StatusCode(500, "An error occurred while updating statistics");
            }
        }

        [HttpGet("top-manga")]
        public async Task<ActionResult<List<Manga>>> GetTopManga(
            [FromQuery] int count = 5, 
            [FromQuery] string period = "all",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 5)
        {
            try
            {
                DateTime? startDate = null;
                DateTime endDate = DateTime.UtcNow;
                
                // Calculate start date based on period
                if (period != "all")
                {
                    startDate = period switch
                    {
                        "day" => endDate.AddDays(-1),
                        "week" => endDate.AddDays(-7),
                        "month" => endDate.AddMonths(-1),
                        "year" => endDate.AddYears(-1),
                        _ => null // Default to all time
                    };
                }
                
                // Determine if we should use count or pagination
                if (count > 0 && page == 1)
                {
                    // Use count for simple top N queries
                    var topManga = startDate.HasValue 
                        ? await _statsService.GetTopMangaByTimeRangeAsync(startDate.Value, endDate, count)
                        : await _statsService.GetTopViewedMangaAsync(count);
                    
                    return Ok(new { 
                        manga = topManga,
                        totalCount = topManga.Count,
                        totalPages = 1,
                        currentPage = 1
                    });
                }
                else
                {
                    // Use pagination
                    var query = startDate.HasValue
                        ? _dbContext.ViewCounts
                            .Where(v => v.ViewedAt >= startDate.Value && v.ViewedAt <= endDate)
                            .GroupBy(v => v.MangaId)
                            .Select(g => new { MangaId = g.Key, ViewCount = g.Count() })
                            .OrderByDescending(x => x.ViewCount)
                        : _dbContext.ViewCounts
                            .GroupBy(v => v.MangaId)
                            .Select(g => new { MangaId = g.Key, ViewCount = g.Count() })
                            .OrderByDescending(x => x.ViewCount);
                    
                    // Get total count for pagination
                    var totalCount = await query.CountAsync();
                    var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                    // Apply pagination
                    var paginatedIds = await query
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();
                    
                    // Get the corresponding manga objects
                    var mangaIds = paginatedIds.Select(v => v.MangaId).ToList();
                    var mangas = await _dbContext.Mangas
                        .Where(m => mangaIds.Contains(m.MangaId))
                        .ToListAsync();
                    
                    // Sort and merge with view counts
                    var result = mangas
                        .Join(paginatedIds,
                            m => m.MangaId,
                            v => v.MangaId,
                            (manga, viewData) => new
                            {
                                Manga = manga,
                                ViewCount = viewData.ViewCount
                            })
                        .OrderByDescending(x => x.ViewCount)
                        .Select(x =>
                        {
                            if (x.Manga.ViewCount == null || x.Manga.ViewCount < x.ViewCount)
                                x.Manga.ViewCount = x.ViewCount;
                            return x.Manga;
                        })
                        .ToList();
                    
                    return Ok(new { 
                        manga = result,
                        totalCount = totalCount,
                        totalPages = totalPages,
                        currentPage = page
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving top manga");
                return StatusCode(500, "An error occurred while retrieving top manga");
            }
        }
        
        [HttpGet("top-favorites")]
        public async Task<ActionResult<List<Manga>>> GetTopFavorites(
            [FromQuery] int count = 5, 
            [FromQuery] string period = "all",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 5)
        {
            try
            {
                // Determine timeframe based on period
                DateTime? startDate = null;
                if (period != "all")
                {
                    startDate = period switch
                    {
                        "day" => DateTime.UtcNow.AddDays(-1),
                        "week" => DateTime.UtcNow.AddDays(-7),
                        "month" => DateTime.UtcNow.AddMonths(-1),
                        "year" => DateTime.UtcNow.AddYears(-1),
                        _ => null // Default to all time
                    };
                }
                
                // If using the service for a specific time period
                if (startDate.HasValue)
                {
                    try
                    {
                        // Use count for simple top N queries without pagination
                        if (count > 0 && page == 1)
                        {
                            var topFavorites = await _statsService.GetTopFavoritesByTimeRangeAsync(
                                startDate.Value, 
                                DateTime.UtcNow, 
                                count);
                            
                            return Ok(new { 
                                manga = topFavorites,
                                totalCount = topFavorites.Count,
                                totalPages = 1,
                                currentPage = 1
                            });
                        }
                        else
                        {
                            // For pagination, we need to do a bit more work
                            // First get all favorites in the time range to count them
                            var allFavorites = await _dbContext.Favorites
                                .Where(f => f.CreatedAt >= startDate.Value)
                                .GroupBy(f => f.MangaId)
                                .Select(g => new { MangaId = g.Key, FavoriteCount = g.Count() })
                                .OrderByDescending(x => x.FavoriteCount)
                                .ToListAsync();
                            
                            var totalCount = allFavorites.Count;
                            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                            
                            // Then get just the page we need
                            var paginatedFavorites = allFavorites
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();
                            
                            // Get the corresponding manga objects
                            var mangaIds = paginatedFavorites.Select(f => f.MangaId).ToList();
                            var mangas = await _dbContext.Mangas
                                .Where(m => mangaIds.Contains(m.MangaId))
                                .ToListAsync();
                            
                            // Sort and merge with favorite counts
                            var result = mangas
                                .Join(paginatedFavorites, 
                                    m => m.MangaId, 
                                    f => f.MangaId, 
                                    (manga, favorite) => new 
                                    {
                                        Manga = manga,
                                        FavoriteCount = favorite.FavoriteCount
                                    })
                                .OrderByDescending(x => x.FavoriteCount)
                                .Select(x => 
                                {
                                    x.Manga.FavoriteCount = x.FavoriteCount;
                                    return x.Manga;
                                })
                                .ToList();
                            
                            return Ok(new { 
                                manga = result,
                                totalCount = totalCount,
                                totalPages = totalPages,
                                currentPage = page
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error using StatsService for top favorites");
                        // Fall back to database query approach if service fails
                    }
                }
                
                // If we get here, either period is "all" or the service attempt failed
                // Create base query for all-time favorites
                var baseQuery = _dbContext.Favorites
                    .GroupBy(f => f.MangaId)
                    .Select(g => new { MangaId = g.Key, FavoriteCount = g.Count() })
                    .OrderByDescending(x => x.FavoriteCount);
                
                // Use count for simple top N queries
                if (count > 0 && page == 1)
                {
                    var topFavorites = await baseQuery.Take(count).ToListAsync();
                    
                    // Get the corresponding manga objects
                    var mangaIds = topFavorites.Select(f => f.MangaId).ToList();
                    var mangas = await _dbContext.Mangas
                        .Where(m => mangaIds.Contains(m.MangaId))
                        .ToListAsync();
                    
                    // Sort and merge with favorite counts
                    var result = mangas
                        .Join(topFavorites, 
                            m => m.MangaId, 
                            f => f.MangaId, 
                            (manga, favorite) => new 
                            {
                                Manga = manga,
                                FavoriteCount = favorite.FavoriteCount
                            })
                        .OrderByDescending(x => x.FavoriteCount)
                        .Select(x => 
                        {
                            x.Manga.FavoriteCount = x.FavoriteCount;
                            return x.Manga;
                        })
                        .ToList();
                    
                    return Ok(new { 
                        manga = result,
                        totalCount = topFavorites.Count,
                        totalPages = 1,
                        currentPage = 1
                    });
                }
                else
                {
                    // Get total count for pagination
                    var totalCount = await baseQuery.CountAsync();
                    var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                    
                    // Apply pagination
                    var paginatedIds = await baseQuery
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();
                    
                    // Get the corresponding manga objects
                    var mangaIds = paginatedIds.Select(f => f.MangaId).ToList();
                    var mangas = await _dbContext.Mangas
                        .Where(m => mangaIds.Contains(m.MangaId))
                        .ToListAsync();
                    
                    // Sort and merge with favorite counts
                    var result = mangas
                        .Join(paginatedIds, 
                            m => m.MangaId, 
                            f => f.MangaId, 
                            (manga, favorite) => new 
                            {
                                Manga = manga,
                                FavoriteCount = favorite.FavoriteCount
                            })
                        .OrderByDescending(x => x.FavoriteCount)
                        .Select(x => 
                        {
                            x.Manga.FavoriteCount = x.FavoriteCount;
                            return x.Manga;
                        })
                        .ToList();
                    
                    return Ok(new { 
                        manga = result,
                        totalCount = totalCount,
                        totalPages = totalPages,
                        currentPage = page
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving top favorites");
                return StatusCode(500, "An error occurred while retrieving top favorites");
            }
        }
        
        [HttpGet("genre-distribution")]
        public async Task<ActionResult> GetGenreDistribution()
        {
            try
            {
                var genreCounts = await _dbContext.MangaGenres
                    .GroupBy(mg => mg.GenreId)
                    .Select(g => new { GenreId = g.Key, Count = g.Count() })
                    .ToListAsync();

                var totalCount = genreCounts.Sum(g => g.Count);

                var genres = await _dbContext.Genres.ToListAsync();

                var labels = new List<string>();
                var data = new List<int>();

                foreach (var genreCount in genreCounts)
                {
                    var genre = genres.FirstOrDefault(g => g.GenreId == genreCount.GenreId);
                    if (genre != null)
                    {
                        labels.Add(genre.GenreName);
                        // Calculate percentage and round to whole number
                        var percentage = (int)Math.Round((double)genreCount.Count / totalCount * 100);
                        data.Add(percentage);
                    }
                }

                return Ok(new { labels, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving genre distribution");
                return StatusCode(500, "An error occurred while retrieving genre distribution");
                
                // Return sample data in case of error
                var sampleLabels = new[] { "Hành động", "Tình cảm", "Hài hước", "Phiêu lưu", "Kinh dị" };
                var sampleData = new[] { 35, 25, 20, 10, 10 };
                
                return Ok(new { labels = sampleLabels, data = sampleData });
            }
        }

        [HttpGet("traffic-data")]
        public async Task<ActionResult> GetTrafficData([FromQuery] int days = 14)
        {
            try
            {
                var endDate = DateTime.Now;
                var startDate = endDate.AddDays(-days);
                
                // Lấy tất cả lượt xem trong khoảng thời gian
                var viewCounts = await _dbContext.ViewCounts
                    .Where(v => v.ViewedAt >= startDate && v.ViewedAt <= endDate)
                    .ToListAsync();
                
                // Nhóm theo ngày
                var dailyViews = viewCounts
                    .GroupBy(v => v.ViewedAt.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Date)
                    .ToDictionary(x => x.Date, x => x.Count);
                
                // Tạo danh sách ngày liên tục
                var labels = new List<string>();
                var values = new List<int>();
                
                for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
                {
                    labels.Add(date.ToString("dd/MM"));
                    values.Add(dailyViews.ContainsKey(date) ? dailyViews[date] : 0);
                }
                
                return Ok(new { labels, values });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving traffic data");
                return StatusCode(500, "An error occurred while retrieving traffic data");
            }
        }

        [HttpGet("user-growth")]
        public async Task<ActionResult> GetUserGrowthData([FromQuery] int months = 6)
        {
            try
            {
                var endDate = DateTime.Now;
                var startDate = endDate.AddMonths(-months);
                
                // Lấy tất cả người dùng được tạo trong khoảng thời gian
                var users = await _dbContext.Users
                    .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                    .ToListAsync();
                
                // Nhóm theo tháng
                var monthlyUsers = users
                    .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                    .Select(g => new { 
                        Year = g.Key.Year, 
                        Month = g.Key.Month, 
                        Count = g.Count() 
                    })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToList();
                
                // Tạo danh sách tháng liên tục
                var labels = new List<string>();
                var values = new List<int>();
                
                for (var date = new DateTime(startDate.Year, startDate.Month, 1); 
                     date <= new DateTime(endDate.Year, endDate.Month, 1); 
                     date = date.AddMonths(1))
                {
                    string monthName = "Tháng " + date.Month;
                    labels.Add(monthName);
                    
                    var monthData = monthlyUsers.FirstOrDefault(m => m.Year == date.Year && m.Month == date.Month);
                    values.Add(monthData?.Count ?? 0);
                }
                
                return Ok(new { labels, values });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user growth data");
                return StatusCode(500, "An error occurred while retrieving user growth data");
            }
        }
    }
}