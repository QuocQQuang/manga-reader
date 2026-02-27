using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mangareading.Models;
using Mangareading.Repositories;

namespace Mangareading.Services
{
    public interface IMangaStatisticsService
    {
        Task<object> GetMangaStatisticsAsync(int mangaId, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<object>> GetDailyViewsAsync(int mangaId, DateTime startDate, DateTime endDate);
        Task<List<object>> GetMonthlyViewsAsync(int mangaId, int year);
        Task<List<object>> GetYearlyViewsAsync(int mangaId);
    }

    public class MangaStatisticsService : IMangaStatisticsService
    {
        private readonly YourDbContext _context;
        private readonly IViewCountRepository _viewCountRepository;

        public MangaStatisticsService(YourDbContext context, IViewCountRepository viewCountRepository)
        {
            _context = context;
            _viewCountRepository = viewCountRepository;
        }

        public async Task<object> GetMangaStatisticsAsync(int mangaId, DateTime? startDate = null, DateTime? endDate = null)
        {
            // Set default date range if not provided
            startDate ??= DateTime.UtcNow.AddMonths(-1);
            endDate ??= DateTime.UtcNow;

            // Get total view count
            var totalViews = await _viewCountRepository.GetMangaViewCountAsync(mangaId);

            // Get view count in date range
            var viewsInRange = await _context.ViewCounts
                .Where(v => v.MangaId == mangaId &&
                       v.ViewedAt >= startDate &&
                       v.ViewedAt <= endDate)
                .CountAsync();

            // Get favorite count
            var favoriteCount = await _context.Favorites
                .Where(f => f.MangaId == mangaId)
                .CountAsync();

            // Get chapter count
            var chapterCount = await _context.Chapters
                .Where(c => c.MangaId == mangaId)
                .CountAsync();

            // Get most viewed chapters
            var mostViewedChapters = await _context.ViewCounts
                .Where(v => v.MangaId == mangaId)
                .GroupBy(v => v.ChapterId)
                .Select(g => new
                {
                    ChapterId = g.Key,
                    ViewCount = g.Count()
                })
                .OrderByDescending(x => x.ViewCount)
                .Take(5)
                .ToListAsync();

            var chapterIds = mostViewedChapters.Select(c => c.ChapterId).ToList();
            var chapters = await _context.Chapters
                .Where(c => chapterIds.Contains(c.ChapterId))
                .ToDictionaryAsync(c => c.ChapterId, c => c.Title ?? $"Chapter {c.ChapterNumber}");

            var topChapters = mostViewedChapters.Select(c => new
            {
                ChapterId = c.ChapterId,
                Title = chapters.ContainsKey(c.ChapterId) ? chapters[c.ChapterId] : $"Chapter {c.ChapterId}",
                Views = c.ViewCount
            }).ToList();

            return new
            {
                TotalViews = totalViews,
                ViewsInRange = viewsInRange,
                FavoriteCount = favoriteCount,
                ChapterCount = chapterCount,
                TopChapters = topChapters,
                DateRange = new
                {
                    StartDate = startDate,
                    EndDate = endDate
                }
            };
        }

        public async Task<List<object>> GetDailyViewsAsync(int mangaId, DateTime startDate, DateTime endDate)
        {
            // Ensure the dates cover whole days
            startDate = startDate.Date;
            endDate = endDate.Date.AddDays(1).AddSeconds(-1);

            var dailyViews = await _context.ViewCounts
                .Where(v => v.MangaId == mangaId &&
                       v.ViewedAt >= startDate &&
                       v.ViewedAt <= endDate)
                .GroupBy(v => v.ViewedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Views = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            // Fill in any missing days with zero counts
            var result = new List<object>();
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var dailyView = dailyViews.FirstOrDefault(d => d.Date.Date == date.Date);
                result.Add(new
                {
                    Date = date.ToString("yyyy-MM-dd"),
                    Views = dailyView?.Views ?? 0
                });
            }

            return result;
        }

        public async Task<List<object>> GetMonthlyViewsAsync(int mangaId, int year)
        {
            var startDate = new DateTime(year, 1, 1);
            var endDate = new DateTime(year, 12, 31, 23, 59, 59);

            var monthlyViews = await _context.ViewCounts
                .Where(v => v.MangaId == mangaId &&
                       v.ViewedAt >= startDate &&
                       v.ViewedAt <= endDate)
                .GroupBy(v => new { v.ViewedAt.Year, v.ViewedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Views = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            // Fill in any missing months with zero counts
            var result = new List<object>();
            for (int month = 1; month <= 12; month++)
            {
                var monthView = monthlyViews.FirstOrDefault(m => m.Month == month);
                result.Add(new
                {
                    Year = year,
                    Month = month,
                    Views = monthView?.Views ?? 0
                });
            }

            return result;
        }

        public async Task<List<object>> GetYearlyViewsAsync(int mangaId)
        {
            // Get the earliest year for this manga
            var firstView = await _context.ViewCounts
                .Where(v => v.MangaId == mangaId)
                .OrderBy(v => v.ViewedAt)
                .FirstOrDefaultAsync();

            if (firstView == null)
            {
                return new List<object>();
            }

            int startYear = firstView.ViewedAt.Year;
            int currentYear = DateTime.Now.Year;

            var yearlyViews = await _context.ViewCounts
                .Where(v => v.MangaId == mangaId)
                .GroupBy(v => v.ViewedAt.Year)
                .Select(g => new
                {
                    Year = g.Key,
                    Views = g.Count()
                })
                .OrderBy(x => x.Year)
                .ToListAsync();

            // Fill in any missing years with zero counts
            var result = new List<object>();
            for (int year = startYear; year <= currentYear; year++)
            {
                var yearView = yearlyViews.FirstOrDefault(y => y.Year == year);
                result.Add(new
                {
                    Year = year,
                    Views = yearView?.Views ?? 0
                });
            }

            return result;
        }
    }
}
