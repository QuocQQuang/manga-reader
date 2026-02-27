using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mangareading.Models;

namespace Mangareading.Repositories
{
    public interface IReadingHistoryRepository
    {
        Task AddToHistoryAsync(int userId, int mangaId, int chapterId);
        Task<List<ReadingHistory>> GetUserHistoryAsync(int userId, int limit = 20);
        Task<ReadingHistory> GetLastReadChapterAsync(int userId, int mangaId);
        Task ClearHistoryAsync(int userId);
        Task ClearMangaHistoryAsync(int userId, int mangaId);
    }

    public class ReadingHistoryRepository : IReadingHistoryRepository
    {
        private readonly YourDbContext _context;

        public ReadingHistoryRepository(YourDbContext context)
        {
            _context = context;
        }

        public async Task AddToHistoryAsync(int userId, int mangaId, int chapterId)
        {
            var existingHistory = await _context.ReadingHistories
                .FirstOrDefaultAsync(h => h.UserId == userId && h.MangaId == mangaId && h.ChapterId == chapterId);
            
            if (existingHistory != null)
            {
                // Update existing record with new timestamp
                existingHistory.ReadAt = DateTime.UtcNow;
            }
            else
            {
                // Create new history record
                var history = new ReadingHistory
                {
                    UserId = userId,
                    MangaId = mangaId,
                    ChapterId = chapterId,
                    ReadAt = DateTime.UtcNow
                };
                
                _context.ReadingHistories.Add(history);
            }
            
            await _context.SaveChangesAsync();
        }

        public async Task<List<ReadingHistory>> GetUserHistoryAsync(int userId, int limit = 20)
        {
            // Get the most recent reading history entries for each manga
            var latestHistoryByManga = await _context.ReadingHistories
                .Where(h => h.UserId == userId)
                .GroupBy(h => h.MangaId)
                .Select(g => g.OrderByDescending(x => x.ReadAt).FirstOrDefault())
                .ToListAsync();

            // Get the IDs of these history entries
            var historyIds = latestHistoryByManga.Select(h => h.HistoryId).ToList();

            // Now fetch the complete data with includes
            var history = await _context.ReadingHistories
                .Where(h => historyIds.Contains(h.HistoryId))
                .Include(h => h.Manga)
                .Include(h => h.Chapter)
                .OrderByDescending(h => h.ReadAt)
                .Take(limit)
                .ToListAsync();

            return history;
        }

        public async Task<ReadingHistory> GetLastReadChapterAsync(int userId, int mangaId)
        {
            return await _context.ReadingHistories
                .Where(h => h.UserId == userId && h.MangaId == mangaId)
                .OrderByDescending(h => h.ReadAt)
                .Include(h => h.Chapter)
                .FirstOrDefaultAsync();
        }

        public async Task ClearHistoryAsync(int userId)
        {
            var userHistory = await _context.ReadingHistories
                .Where(h => h.UserId == userId)
                .ToListAsync();

            _context.ReadingHistories.RemoveRange(userHistory);
            await _context.SaveChangesAsync();
        }

        public async Task ClearMangaHistoryAsync(int userId, int mangaId)
        {
            var mangaHistory = await _context.ReadingHistories
                .Where(h => h.UserId == userId && h.MangaId == mangaId)
                .ToListAsync();

            _context.ReadingHistories.RemoveRange(mangaHistory);
            await _context.SaveChangesAsync();
        }
    }
}