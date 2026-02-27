using Mangareading.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mangareading.Services
{
    public class StatisticsService
    {
        private readonly YourDbContext _context;

        public StatisticsService(YourDbContext context)
        {
            _context = context;
        }

        // Record a manga view when a user/visitor views a chapter
        public async Task RecordMangaViewAsync(int mangaId, int chapterId, int? userId, string ipAddress)
        {
            var view = new MangaView
            {
                MangaId = mangaId,
                ChapterId = chapterId,
                UserId = userId,
                IpAddress = ipAddress,
                ViewedAt = DateTime.UtcNow
            };

            _context.MangaViews.Add(view);
            
            // Also record in reading history if user is logged in
            if (userId.HasValue)
            {
                var existingHistory = await _context.ReadingHistories
                    .FirstOrDefaultAsync(h => h.UserId == userId.Value && h.MangaId == mangaId && h.ChapterId == chapterId);
                
                if (existingHistory != null)
                {
                    // Update existing history
                    existingHistory.ReadAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new history entry
                    var history = new ReadingHistory
                    {
                        UserId = userId.Value,
                        MangaId = mangaId,
                        ChapterId = chapterId,
                        ReadAt = DateTime.UtcNow
                    };
                    _context.ReadingHistories.Add(history);
                }
            }

            await _context.SaveChangesAsync();
        }

        // Get view statistics by time period (day, month, year)
        public async Task<Dictionary<DateTime, int>> GetViewStatisticsByPeriodAsync(int mangaId, string period, DateTime? startDate = null, DateTime? endDate = null)
        {
            if (!startDate.HasValue)
                startDate = DateTime.UtcNow.AddMonths(-1);
            
            if (!endDate.HasValue)
                endDate = DateTime.UtcNow;

            var views = await _context.MangaViews
                .Where(v => v.MangaId == mangaId && v.ViewedAt >= startDate && v.ViewedAt <= endDate)
                .ToListAsync();

            return GroupViewsByPeriod(views, period);
        }

        // Get favorite statistics by time period (day, month, year)
        public async Task<Dictionary<DateTime, int>> GetFavoriteStatisticsByPeriodAsync(int mangaId, string period, DateTime? startDate = null, DateTime? endDate = null)
        {
            if (!startDate.HasValue)
                startDate = DateTime.UtcNow.AddMonths(-1);
            
            if (!endDate.HasValue)
                endDate = DateTime.UtcNow;

            var favorites = await _context.Favorites
                .Where(f => f.MangaId == mangaId && f.CreatedAt >= startDate && f.CreatedAt <= endDate)
                .ToListAsync();

            return GroupFavoritesByPeriod(favorites, period);
        }

        // Get user's reading history
        public async Task<List<ReadingHistory>> GetUserReadingHistoryAsync(int userId, int? limit = 20)
        {
            IQueryable<ReadingHistory> query = _context.ReadingHistories
                .Include(h => h.Manga)
                .Include(h => h.Chapter)
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.ReadAt);

            if (limit.HasValue)
                query = query.Take(limit.Value);

            return await query.ToListAsync();
        }

        // Get total views for a manga
        public async Task<int> GetTotalMangaViewsAsync(int mangaId)
        {
            return await _context.MangaViews.CountAsync(v => v.MangaId == mangaId);
        }

        // Get total favorites for a manga
        public async Task<int> GetTotalMangaFavoritesAsync(int mangaId)
        {
            return await _context.Favorites.CountAsync(f => f.MangaId == mangaId);
        }

        // Get manga view counts for dashboard/admin
        public async Task<List<(Manga Manga, int ViewCount)>> GetTopViewedMangaAsync(int limit = 10)
        {
            var result = await _context.MangaViews
                .GroupBy(v => v.MangaId)
                .Select(g => new
                {
                    MangaId = g.Key,
                    ViewCount = g.Count()
                })
                .OrderByDescending(x => x.ViewCount)
                .Take(limit)
                .ToListAsync();

            var mangaIds = result.Select(r => r.MangaId).ToList();
            var mangas = await _context.Mangas.Where(m => mangaIds.Contains(m.MangaId)).ToListAsync();

            return result.Select(r => (
                mangas.First(m => m.MangaId == r.MangaId),
                r.ViewCount
            )).ToList();
        }

        #region Helper Methods
        private Dictionary<DateTime, int> GroupViewsByPeriod(List<MangaView> views, string period)
        {
            var result = new Dictionary<DateTime, int>();

            foreach (var view in views)
            {
                DateTime groupKey;
                
                switch (period.ToLower())
                {
                    case "day":
                        groupKey = view.ViewedAt.Date;
                        break;
                    case "month":
                        groupKey = new DateTime(view.ViewedAt.Year, view.ViewedAt.Month, 1);
                        break;
                    case "year":
                        groupKey = new DateTime(view.ViewedAt.Year, 1, 1);
                        break;
                    default:
                        groupKey = view.ViewedAt.Date;
                        break;
                }

                if (result.ContainsKey(groupKey))
                    result[groupKey]++;
                else
                    result[groupKey] = 1;
            }

            return result.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        private Dictionary<DateTime, int> GroupFavoritesByPeriod(List<Favorite> favorites, string period)
        {
            var result = new Dictionary<DateTime, int>();

            foreach (var favorite in favorites)
            {
                DateTime groupKey;
                
                switch (period.ToLower())
                {
                    case "day":
                        groupKey = favorite.CreatedAt.Date;
                        break;
                    case "month":
                        groupKey = new DateTime(favorite.CreatedAt.Year, favorite.CreatedAt.Month, 1);
                        break;
                    case "year":
                        groupKey = new DateTime(favorite.CreatedAt.Year, 1, 1);
                        break;
                    default:
                        groupKey = favorite.CreatedAt.Date;
                        break;
                }

                if (result.ContainsKey(groupKey))
                    result[groupKey]++;
                else
                    result[groupKey] = 1;
            }

            return result.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value);
        }
        #endregion
    }
}