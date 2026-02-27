using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mangareading.Models;
using Mangareading.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mangareading.Services
{
    public class StatsService : IStatsService
    {
        private readonly YourDbContext _context;
        private readonly ILogger<StatsService> _logger;

        public StatsService(YourDbContext context, ILogger<StatsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task UpdateStatsAsync()
        {
            try
            {
                _logger.LogInformation("Updating statistics data");
                
                // Cập nhật thống kê tổng quan
                await UpdateViewCountStatsAsync();
                
                _logger.LogInformation("Statistics data updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating statistics data");
                throw;
            }
        }
        
        private async Task UpdateViewCountStatsAsync()
        {
            try
            {
                // Cập nhật view count cho tất cả manga
                var mangas = await _context.Mangas.ToListAsync();
                
                foreach (var manga in mangas)
                {
                    // Tính tổng view count cho manga từ bảng ViewCount
                    var viewCount = await _context.ViewCounts
                        .Where(vc => vc.MangaId == manga.MangaId)
                        .CountAsync();
                    
                    manga.ViewCount = viewCount;
                }
                
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated view count statistics for {Count} manga", mangas.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating view count statistics");
                throw;
            }
        }
        
        public async Task<object> GetBasicStatsAsync()
        {
            try
            {
                var totalUsers = await _context.Users.CountAsync();
                var totalManga = await _context.Mangas.CountAsync();
                var totalChapters = await _context.Chapters.CountAsync();
                var totalViews = await _context.Mangas.SumAsync(m => m.ViewCount);
                
                return new
                {
                    TotalUsers = totalUsers,
                    TotalManga = totalManga,
                    TotalChapters = totalChapters,
                    TotalViews = totalViews
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting basic statistics");
                throw;
            }
        }
        
        public async Task<List<Manga>> GetTopViewedMangaAsync(int count = 5)
        {
            try
            {
                return await _context.Mangas
                    .OrderByDescending(m => m.ViewCount)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top viewed manga");
                throw;
            }
        }
        
        public async Task<Dictionary<string, int>> GetGenreDistributionAsync()
        {
            try
            {
                var genreCounts = await _context.MangaGenres
                    .GroupBy(mg => mg.GenreId)
                    .Select(g => new { GenreId = g.Key, Count = g.Count() })
                    .ToListAsync();
                
                var genres = await _context.Genres.ToListAsync();
                
                var distribution = new Dictionary<string, int>();
                
                foreach (var genreCount in genreCounts)
                {
                    var genre = genres.FirstOrDefault(g => g.GenreId == genreCount.GenreId);
                    if (genre != null)
                    {
                        distribution[genre.GenreName] = genreCount.Count;
                    }
                }
                
                return distribution;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting genre distribution");
                throw;
            }
        }
        
        // New method implementations for time-based statistics
        
        public async Task<object> GetStatsByTimeRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                // Ensure endDate is inclusive by adding one day and then subtracting a tick
                endDate = endDate.AddDays(1).AddTicks(-1);
                
                // Count users registered in this period
                var newUsers = await _context.Users
                    .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                    .CountAsync();
                
                // Count manga added in this period
                var newManga = await _context.Mangas
                    .Where(m => m.CreatedAt >= startDate && m.CreatedAt <= endDate)
                    .CountAsync();
                
                // Count chapters added in this period
                var newChapters = await _context.Chapters
                    .Where(c => c.UploadDate >= startDate && c.UploadDate <= endDate)
                    .CountAsync();
                
                // Count views in this period
                var viewsInPeriod = await _context.ViewCounts
                    .Where(vc => vc.ViewedAt >= startDate && vc.ViewedAt <= endDate)
                    .CountAsync();
                
                return new
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    NewUsers = newUsers,
                    NewManga = newManga,
                    NewChapters = newChapters,
                    ViewsInPeriod = viewsInPeriod
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics by time range");
                throw;
            }
        }
        
        public async Task<object> GetViewsDataByPeriodAsync(string period)
        {
            try
            {
                DateTime startDate;
                DateTime endDate = DateTime.UtcNow;
                int groupByDays = 1;
                string groupByFormat = "dd/MM";
                
                // Set grouping and range based on period
                switch (period.ToLower())
                {
                    case "day":
                        startDate = endDate.AddDays(-1);
                        groupByFormat = "HH:00"; // Group by hour
                        break;
                    case "week":
                        startDate = endDate.AddDays(-7);
                        break;
                    case "month":
                        startDate = endDate.AddMonths(-1);
                        groupByDays = 1;
                        break;
                    case "year":
                        startDate = endDate.AddYears(-1);
                        groupByDays = 30; // Group by month roughly
                        groupByFormat = "MM/yyyy";
                        break;
                    default:
                        startDate = endDate.AddDays(-30); // Default to month
                        break;
                }
                
                // Get all views in the period
                var views = await _context.ViewCounts
                    .Where(vc => vc.ViewedAt >= startDate && vc.ViewedAt <= endDate)
                    .OrderBy(vc => vc.ViewedAt)
                    .ToListAsync();
                
                // Group views by date according to period
                var groupedViews = views
                    .GroupBy(v => period.ToLower() == "day" 
                        ? v.ViewedAt.ToString(groupByFormat) // For day, group by hour
                        : new DateTime(v.ViewedAt.Year, v.ViewedAt.Month, v.ViewedAt.Day)
                            .AddDays(-(v.ViewedAt.Day % groupByDays)) // Group by days
                            .ToString(groupByFormat))
                    .Select(g => new
                    {
                        Label = g.Key,
                        Count = g.Count()
                    })
                    .ToList();
                
                // Fill in gaps in data
                var result = new List<object>();
                if (period.ToLower() == "day")
                {
                    // For day period, fill in all 24 hours
                    for (int hour = 0; hour < 24; hour++)
                    {
                        string hourLabel = $"{hour:00}:00";
                        var existing = groupedViews.FirstOrDefault(g => g.Label == hourLabel);
                        result.Add(new
                        {
                            Label = hourLabel,
                            Count = existing?.Count ?? 0
                        });
                    }
                }
                else if (period.ToLower() == "year")
                {
                    // For year, fill in all 12 months
                    for (int month = 1; month <= 12; month++)
                    {
                        string monthLabel = $"{month:00}/{endDate.Year}";
                        var existing = groupedViews.FirstOrDefault(g => g.Label == monthLabel);
                        result.Add(new
                        {
                            Label = monthLabel,
                            Count = existing?.Count ?? 0
                        });
                    }
                }
                else
                {
                    // For week and month, fill in each day
                    for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
                    {
                        string dateLabel = date.ToString(groupByFormat);
                        var existing = groupedViews.FirstOrDefault(g => g.Label == dateLabel);
                        result.Add(new
                        {
                            Label = dateLabel,
                            Count = existing?.Count ?? 0
                        });
                    }
                }
                
                return new
                {
                    Period = period,
                    StartDate = startDate,
                    EndDate = endDate,
                    Labels = result.Select(r => ((dynamic)r).Label).ToList(),
                    Data = result.Select(r => ((dynamic)r).Count).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting views data by period");
                throw;
            }
        }
        
        public async Task<object> GetUserGrowthByPeriodAsync(string period)
        {
            try
            {
                DateTime startDate;
                DateTime endDate = DateTime.UtcNow;
                int groupByDays = 1;
                string groupByFormat = "dd/MM";
                
                // Set grouping and range based on period
                switch (period.ToLower())
                {
                    case "day":
                        startDate = endDate.AddDays(-1);
                        groupByFormat = "HH:00"; // Group by hour
                        break;
                    case "week":
                        startDate = endDate.AddDays(-7);
                        break;
                    case "month":
                        startDate = endDate.AddMonths(-1);
                        groupByDays = 1;
                        break;
                    case "year":
                        startDate = endDate.AddYears(-1);
                        groupByDays = 30; // Group by month roughly
                        groupByFormat = "MM/yyyy";
                        break;
                    default:
                        startDate = endDate.AddDays(-30); // Default to month
                        break;
                }
                
                // Get all users created in the period
                var users = await _context.Users
                    .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                    .OrderBy(u => u.CreatedAt)
                    .ToListAsync();
                
                // Group users by date according to period
                var groupedUsers = users
                    .GroupBy(u => period.ToLower() == "day" 
                        ? u.CreatedAt.ToString(groupByFormat) // For day, group by hour
                        : new DateTime(u.CreatedAt.Year, u.CreatedAt.Month, u.CreatedAt.Day)
                            .AddDays(-(u.CreatedAt.Day % groupByDays)) // Group by days
                            .ToString(groupByFormat))
                    .Select(g => new
                    {
                        Label = g.Key,
                        Count = g.Count()
                    })
                    .ToList();
                
                // Fill in gaps in data similar to views data
                var result = new List<object>();
                if (period.ToLower() == "day")
                {
                    // For day period, fill in all 24 hours
                    for (int hour = 0; hour < 24; hour++)
                    {
                        string hourLabel = $"{hour:00}:00";
                        var existing = groupedUsers.FirstOrDefault(g => g.Label == hourLabel);
                        result.Add(new
                        {
                            Label = hourLabel,
                            Count = existing?.Count ?? 0
                        });
                    }
                }
                else if (period.ToLower() == "year")
                {
                    // For year, fill in all 12 months
                    for (int month = 1; month <= 12; month++)
                    {
                        string monthLabel = $"{month:00}/{endDate.Year}";
                        var existing = groupedUsers.FirstOrDefault(g => g.Label == monthLabel);
                        result.Add(new
                        {
                            Label = monthLabel,
                            Count = existing?.Count ?? 0
                        });
                    }
                }
                else
                {
                    // For week and month, fill in each day
                    for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
                    {
                        string dateLabel = date.ToString(groupByFormat);
                        var existing = groupedUsers.FirstOrDefault(g => g.Label == dateLabel);
                        result.Add(new
                        {
                            Label = dateLabel,
                            Count = existing?.Count ?? 0
                        });
                    }
                }
                
                return new
                {
                    Period = period,
                    StartDate = startDate,
                    EndDate = endDate,
                    Labels = result.Select(r => ((dynamic)r).Label).ToList(),
                    Data = result.Select(r => ((dynamic)r).Count).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user growth data by period");
                throw;
            }
        }
        
        public async Task<List<Manga>> GetTopMangaByTimeRangeAsync(DateTime startDate, DateTime endDate, int count = 10)
        {
            try
            {
                // Ensure endDate is inclusive by adding one day and then subtracting a tick
                endDate = endDate.AddDays(1).AddTicks(-1);
                
                // Get view counts grouped by manga in the specified period
                var mangaViewCounts = await _context.ViewCounts
                    .Where(vc => vc.ViewedAt >= startDate && vc.ViewedAt <= endDate)
                    .GroupBy(vc => vc.MangaId)
                    .Select(g => new { MangaId = g.Key, ViewCount = g.Count() })
                    .OrderByDescending(g => g.ViewCount)
                    .Take(count)
                    .ToListAsync();
                
                // Get manga details for the top viewed
                var topManga = new List<Manga>();
                foreach (var item in mangaViewCounts)
                {
                    var manga = await _context.Mangas.FindAsync(item.MangaId);
                    if (manga != null)
                    {
                        // We'll temporarily set ViewCount to the period count
                        manga.ViewCount = item.ViewCount;
                        topManga.Add(manga);
                    }
                }
                
                return topManga;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top manga by time range");
                throw;
            }
        }
        
        public async Task<List<Manga>> GetTopFavoritesByTimeRangeAsync(DateTime startDate, DateTime endDate, int count = 10)
        {
            try
            {
                // Ensure endDate is inclusive by adding one day and then subtracting a tick
                endDate = endDate.AddDays(1).AddTicks(-1);
                
                // Get favorite counts grouped by manga in the specified period
                var mangaFavoriteCounts = await _context.Favorites
                    .Where(f => f.CreatedAt >= startDate && f.CreatedAt <= endDate)
                    .GroupBy(f => f.MangaId)
                    .Select(g => new { MangaId = g.Key, FavoriteCount = g.Count() })
                    .OrderByDescending(g => g.FavoriteCount)
                    .Take(count)
                    .ToListAsync();
                
                // Get manga details for the top favorites
                var topManga = new List<Manga>();
                foreach (var item in mangaFavoriteCounts)
                {
                    var manga = await _context.Mangas.FindAsync(item.MangaId);
                    if (manga != null)
                    {
                        // We'll set the FavoriteCount property (which is not stored in DB)
                        manga.FavoriteCount = item.FavoriteCount;
                        topManga.Add(manga);
                    }
                }
                
                return topManga;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top favorites by time range");
                throw;
            }
        }
    }
}