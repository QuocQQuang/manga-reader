using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mangareading.Models;
using Microsoft.Data.SqlClient;

namespace Mangareading.Repositories
{
    public interface IViewCountRepository
    {
        Task AddViewAsync(int mangaId, int chapterId, int? userId, string ipAddress);
        Task<int> GetMangaViewCountAsync(int mangaId);
        Task<int> GetChapterViewCountAsync(int chapterId);
    }

    public class ViewCountRepository : IViewCountRepository
    {
        private readonly YourDbContext _context;
        private readonly ILogger<ViewCountRepository> _logger;

        public ViewCountRepository(YourDbContext context, ILogger<ViewCountRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AddViewAsync(int mangaId, int chapterId, int? userId, string ipAddress)
        {
            try
            {
                // Nếu không có IP, vẫn tính lượt xem nhưng không lưu IP
                if (string.IsNullOrEmpty(ipAddress))
                {
                    ipAddress = "unknown";
                }

                // Thay đổi query để phù hợp với cấu trúc mới
                int time=1;
                var recentView = await _context.ViewCounts
                    .Where(v => v.ChapterId == chapterId && v.IpAddress == ipAddress && v.ViewedAt > DateTime.UtcNow.AddMinutes(-time))
                    .FirstOrDefaultAsync();

                if (recentView == null)
                {
                    // Add new view record
                    var viewCount = new ViewCount
                    {
                        MangaId = mangaId,
                        ChapterId = chapterId,
                        UserId = userId,
                        IpAddress = ipAddress,
                        ViewedAt = DateTime.UtcNow
                    };

                    try 
                    {
                        _context.ViewCounts.Add(viewCount);
                        
                        // Cập nhật lượt xem trong bảng Manga
                        var manga = await _context.Mangas.FindAsync(mangaId);
                        if (manga != null)
                        {
                            manga.ViewCount = (manga.ViewCount ?? 0) + 1;
                            _context.Mangas.Update(manga);
                        }
                        
                        await _context.SaveChangesAsync();
                        _logger.LogDebug("Added new view for manga {MangaId}, chapter {ChapterId} from IP {IpAddress}", mangaId, chapterId, ipAddress);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Database error while adding view for manga {MangaId}", mangaId);
                        
                        // Trường hợp không tồn tại bảng, tạo bảng
                        if (ex.Message.Contains("Invalid column name") || ex.Message.Contains("doesn't exist"))
                        {
                            try
                            {
                                // Cách giải quyết tạm thời: sử dụng raw SQL
                                string sql = @"
                                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ViewCounts')
                                BEGIN
                                    CREATE TABLE ViewCounts (
                                        MangaId int NOT NULL,
                                        ChapterId int NOT NULL,
                                        UserId int NULL,
                                        IpAddress nvarchar(100) NOT NULL,
                                        ViewedAt datetime2 NOT NULL,
                                        CONSTRAINT PK_ViewCounts PRIMARY KEY (ChapterId, IpAddress, ViewedAt)
                                    );
                                END";
                                await _context.Database.ExecuteSqlRawAsync(sql);
                                
                                // Thử lại thêm lượt xem
                                _context.ViewCounts.Add(viewCount);
                                
                                // Cập nhật lượt xem trong bảng Manga
                                var manga = await _context.Mangas.FindAsync(mangaId);
                                if (manga != null)
                                {
                                    manga.ViewCount = (manga.ViewCount ?? 0) + 1;
                                    _context.Mangas.Update(manga);
                                }
                                
                                await _context.SaveChangesAsync();
                                _logger.LogInformation("Created ViewCounts table and added view record");
                            }
                            catch (Exception innerEx)
                            {
                                _logger.LogError(innerEx, "Failed to create ViewCounts table");
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogDebug("View already recorded for manga {MangaId}, chapter {ChapterId} from IP {IpAddress} within last {Time}", mangaId, chapterId, ipAddress, time);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding view for manga {MangaId}", mangaId);
            }
        }

        public async Task<int> GetMangaViewCountAsync(int mangaId)
        {
            return await _context.ViewCounts
                .Where(v => v.MangaId == mangaId)
                .CountAsync();
        }

        public async Task<int> GetChapterViewCountAsync(int chapterId)
        {
            return await _context.ViewCounts
                .Where(v => v.ChapterId == chapterId)
                .CountAsync();
        }
    }
}
