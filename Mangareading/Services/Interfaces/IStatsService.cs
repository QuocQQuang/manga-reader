using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mangareading.Models;

namespace Mangareading.Services.Interfaces
{
    public interface IStatsService
    {
        Task UpdateStatsAsync();
        Task<object> GetBasicStatsAsync();
        Task<List<Manga>> GetTopViewedMangaAsync(int count = 5);
        Task<Dictionary<string, int>> GetGenreDistributionAsync();
        
        // New methods for time-based statistics
        Task<object> GetStatsByTimeRangeAsync(DateTime startDate, DateTime endDate);
        Task<object> GetViewsDataByPeriodAsync(string period); // period can be 'day', 'week', 'month', 'year'
        Task<object> GetUserGrowthByPeriodAsync(string period); // period can be 'day', 'week', 'month', 'year'
        Task<List<Manga>> GetTopMangaByTimeRangeAsync(DateTime startDate, DateTime endDate, int count = 10);
        Task<List<Manga>> GetTopFavoritesByTimeRangeAsync(DateTime startDate, DateTime endDate, int count = 10);
    }
}