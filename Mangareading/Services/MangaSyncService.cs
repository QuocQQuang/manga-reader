using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mangareading.DTOs;
using Mangareading.Repositories;
using Mangareading.Services;
using Mangareading.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections.Concurrent;

namespace Mangareading.Services
{
    public class MangaSyncService
    {
        private readonly MangaDexService _mangaDexService;
        private readonly IMangaRepository _mangaRepository;
        private readonly IChapterRepository _chapterRepository;
        private readonly ILogger<MangaSyncService> _logger;
        private readonly int _mangadexSourceId = 1; // ID của MangaDex trong bảng Sources
        private readonly YourDbContext _context;
        private readonly ApiCacheService _apiCacheService;

        public MangaSyncService(
            MangaDexService mangaDexService,
            IMangaRepository mangaRepository,
            IChapterRepository chapterRepository,
            ILogger<MangaSyncService> logger,
            YourDbContext context,
            ApiCacheService apiCacheService)
        {
            _mangaDexService = mangaDexService;
            _mangaRepository = mangaRepository;
            _chapterRepository = chapterRepository;
            _logger = logger;
            _context = context;
            _apiCacheService = apiCacheService;
        }
        
        public async Task SyncMangasFromMangaDexAsync(int limit = 50, bool syncChapters = false)
        {
            try
            {
                _logger.LogInformation($"Bắt đầu đồng bộ {limit} truyện từ MangaDex");

                var mangaDtos = await _mangaDexService.GetMangaDexMangaListAsync(limit: limit, language: "vi");

                _logger.LogInformation($"Đã lấy {mangaDtos.Count} truyện từ MangaDex");

                int successCount = 0;
                int totalCount = mangaDtos.Count;

                for (int i = 0; i < totalCount; i += 50)
                {
                    int currentBatchSize = Math.Min(50, totalCount - i);
                    var batch = mangaDtos.OrderByDescending(m => DateTimeOffset.Parse(m.Attributes.UpdatedAt, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal).DateTime)
                                       .Skip(i).Take(currentBatchSize);

                    _logger.LogInformation($"Đồng bộ lô {i / 50 + 1}/{(int)Math.Ceiling((double)totalCount / 50)}, {currentBatchSize} truyện");

                    foreach (var mangaDto in batch)
                    {
                        try
                        {
                            var manga = await _mangaRepository.AddOrUpdateMangaAsync(mangaDto, _mangadexSourceId);
                            successCount++;
                            
                            if (syncChapters)
                            {
                                await SyncChaptersForMangaAsync(mangaDto.Id, manga.MangaId);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Lỗi khi lưu truyện {mangaDto.Id}");
                        }
                    }

                    await Task.Delay(1000);
                }

                _logger.LogInformation($"Đã hoàn thành đồng bộ. Thành công: {successCount}/{totalCount} truyện");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đồng bộ truyện từ MangaDex");
            }
        }
        
        /// <summary>
        /// Đồng bộ truyện tiếng Việt mới nhất từ MangaDex
        /// </summary>
        /// <param name="limit">Số lượng truyện cần đồng bộ</param>
        /// <param name="syncChapters">Có đồng bộ chapter hay không</param>
        public async Task SyncLatestVietnameseMangaAsync(int limit = 10, bool syncChapters = false)
        {
            try
            {
                _logger.LogInformation($"Bắt đầu đồng bộ {limit} truyện tranh tiếng Việt mới nhất (cập nhật gần đây) từ MangaDex");

                var mangaDtos = await _mangaDexService.GetLatestVietnameseMangaAsync(limit);

                _logger.LogInformation($"Đã lấy {mangaDtos.Count} truyện từ MangaDex");

                int successCount = 0;

                foreach (var mangaDto in mangaDtos)
                {
                    try
                    {
                        // Kiểm tra xem truyện đã tồn tại trong database chưa
                        var existingManga = await _context.Mangas
                            .FirstOrDefaultAsync(m => m.ExternalId == mangaDto.Id && m.SourceId == _mangadexSourceId);

                        if (existingManga != null)
                        {
                            // Parse updatedAt từ MangaDex
                            var mangaDexUpdatedAt = DateTimeOffset.Parse(mangaDto.Attributes.UpdatedAt, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal).DateTime;
                            
                            // Nếu updatedAt từ API trùng với updatedAt trong database, bỏ qua truyện này
                            if (existingManga.UpdatedAt == mangaDexUpdatedAt)
                            {
                                _logger.LogInformation($"Bỏ qua truyện {existingManga.Title} (ID: {mangaDto.Id}) vì không có cập nhật mới");
                                successCount++; // Vẫn tính là thành công vì không cần cập nhật
                                continue;
                            }
                        }

                        var manga = await _mangaRepository.AddOrUpdateMangaAsync(mangaDto, _mangadexSourceId);
                        
                        _logger.LogInformation($"Đã lưu truyện: {manga.Title} (ID: {mangaDto.Id})");
                        successCount++;

                        // Cache thông tin truyện vào database
                        string mangaCacheKey = $"manga_{mangaDto.Id}";
                        await _apiCacheService.SaveToCacheAsync(mangaCacheKey, mangaDto, TimeSpan.FromDays(1));

                        if (syncChapters)
                        {
                            _logger.LogInformation($"Bắt đầu đồng bộ chapter cho truyện {manga.Title} (ID: {mangaDto.Id})");
                            await SyncChaptersForMangaAsync(mangaDto.Id, manga.MangaId);
                            
                            // Thêm delay giữa các lần đồng bộ chapter để tránh vượt quá giới hạn rate
                            await Task.Delay(1500);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Lỗi khi lưu truyện {mangaDto.Id}");
                    }
                }

                _logger.LogInformation($"Đã hoàn thành đồng bộ. Thành công: {successCount}/{mangaDtos.Count} truyện");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong quá trình đồng bộ truyện từ MangaDex");
                throw;
            }
        }

        /// <summary>
        /// Đồng bộ truyện từ MangaDex theo ID bên ngoài
        /// </summary>
        /// <param name="externalId">ID của truyện trên MangaDex</param>
        /// <param name="syncChapters">Có đồng bộ chapter hay không</param>
        /// <returns>Thông tin truyện đã đồng bộ</returns>
        public async Task<Manga> SyncMangaByExternalIdAsync(string externalId, bool syncChapters = true)
        {
            try
            {
                _logger.LogInformation($"Bắt đầu đồng bộ truyện từ MangaDex ID: {externalId}");
                
                // Lấy thông tin truyện từ MangaDex
                var mangaDto = await _mangaDexService.GetMangaByIdAsync(externalId);
                
                if (mangaDto == null)
                {
                    _logger.LogWarning($"Không tìm thấy truyện với ID {externalId} trên MangaDex");
                    return null;
                }
                
                // Thêm hoặc cập nhật truyện trong cơ sở dữ liệu
                var manga = await _mangaRepository.AddOrUpdateMangaAsync(mangaDto, _mangadexSourceId);
                
                if (syncChapters)
                {
                    await SyncChaptersForMangaAsync(externalId, manga.MangaId);
                }
                
                _logger.LogInformation($"Đã đồng bộ thành công truyện {manga.Title} (ID: {externalId})");
                
                return manga;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi đồng bộ truyện với ID {externalId}");
                throw;
            }
        }
        
        /// <summary>
        /// Đồng bộ truyện từ MangaDex theo URL
        /// </summary>
        /// <param name="url">URL của truyện trên MangaDex</param>
        /// <param name="syncChapters">Có đồng bộ chapter hay không</param>
        /// <returns>Thông tin truyện đã đồng bộ</returns>
        public async Task<Manga> SyncMangaByUrlAsync(string url, bool syncChapters = true)
        {
            try
            {
                // Trích xuất ID từ URL MangaDex
                var externalId = ExtractMangaDexIdFromUrl(url);
                
                if (string.IsNullOrEmpty(externalId))
                {
                    _logger.LogWarning($"Không thể trích xuất ID từ URL: {url}");
                    return null;
                }
                
                return await SyncMangaByExternalIdAsync(externalId, syncChapters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi đồng bộ truyện từ URL {url}");
                throw;
            }
        }
        
        public async Task<List<Manga>> SyncMangaByNameAsync(string name, bool syncChapters = true, int maxResults = 5)
        {
            try
            {
                _logger.LogInformation($"Bắt đầu tìm kiếm và đồng bộ truyện theo tên: {name}");
                
                // Tìm kiếm truyện theo tên trên MangaDex
                var searchResults = await _mangaDexService.SearchMangaByTitleAsync(name, maxResults);
                
                if (searchResults == null || !searchResults.Any())
                {
                    _logger.LogWarning($"Không tìm thấy truyện nào với tên {name} trên MangaDex");
                    return new List<Manga>();
                }
                
                _logger.LogInformation($"Đã tìm thấy {searchResults.Count} truyện phù hợp với tên: {name}");
                
                var syncedMangas = new List<Manga>();
                
                foreach (var mangaDto in searchResults)
                {
                    try
                    {
                        // Thêm hoặc cập nhật truyện trong cơ sở dữ liệu
                        var manga = await _mangaRepository.AddOrUpdateMangaAsync(mangaDto, _mangadexSourceId);
                        
                        if (syncChapters)
                        {
                            await SyncChaptersForMangaAsync(mangaDto.Id, manga.MangaId);
                        }
                        
                        syncedMangas.Add(manga);
                        _logger.LogInformation($"Đã đồng bộ thành công truyện {manga.Title} (ID: {mangaDto.Id})");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Lỗi khi đồng bộ truyện {mangaDto.Id}");
                    }
                    
                    await Task.Delay(500); // Tạm dừng để tránh gửi quá nhiều request
                }
                
                return syncedMangas;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tìm kiếm và đồng bộ truyện theo tên: {name}");
                throw;
            }
        }

        /// <summary>
        /// Đồng bộ chapter cho một truyện theo ID nội bộ
        /// </summary>
        /// <param name="mangaId">ID của truyện trong database</param>
        public async Task SyncChaptersForMangaByIdAsync(int mangaId)
        {
            try
            {
                var manga = await _mangaRepository.GetByIdAsync(mangaId);
                if (manga == null)
                {
                    _logger.LogWarning($"Không tìm thấy truyện với ID {mangaId}");
                    return;
                }
                
                await SyncChaptersForMangaAsync(manga.ExternalId, manga.MangaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi đồng bộ chapter cho truyện ID {mangaId}");
            }
        }

        /// <summary>
        /// Đồng bộ các chapter cho một truyện
        /// </summary>
        /// <param name="externalMangaId">ID của truyện trên MangaDex</param>
        /// <param name="internalMangaId">ID của truyện trong database</param>
        private async Task SyncChaptersForMangaAsync(string externalMangaId, int internalMangaId)
        {
            try
            {
                _logger.LogInformation($"Bắt đầu đồng bộ chapter cho truyện {externalMangaId}");
                
                // Kiểm tra truyện trong database
                var manga = await _context.Mangas.FindAsync(internalMangaId);
                if (manga == null)
                {
                    _logger.LogWarning($"Không tìm thấy truyện với ID {internalMangaId} trong database");
                    return;
                }
                
                // Kiểm tra nhanh xem cache key có tồn tại
                string mangaCacheKey = $"manga_{externalMangaId}";
                bool hasMangaCache = await _apiCacheService.CacheExistsAsync(mangaCacheKey);
                
                // Lấy thông tin manga (từ cache hoặc API)
                var mangaDto = await _mangaDexService.GetMangaByIdAsync(externalMangaId);
                if (mangaDto == null)
                {
                    _logger.LogWarning($"Không thể lấy thông tin manga với ID {externalMangaId}");
                    return;
                }
                
                // Parse updatedAt từ MangaDex
                var mangaUpdatedAt = DateTimeOffset.Parse(mangaDto.Attributes.UpdatedAt, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal).DateTime;
                
                // Nếu updatedAt từ API trùng với updatedAt trong database, bỏ qua việc đồng bộ chapter
                if (manga.UpdatedAt == mangaUpdatedAt)
                {
                    _logger.LogInformation($"Bỏ qua đồng bộ chapter cho truyện {manga.Title} (ID: {externalMangaId}) vì không có cập nhật mới");
                    return;
                }
                
                _logger.LogInformation($"Cần đồng bộ chapter do manga có cập nhật mới (DB: {manga.UpdatedAt}, API: {mangaUpdatedAt})");
                
                // Kiểm tra nhanh xem cache danh sách chapter có tồn tại
                string chaptersCacheKey = $"manga_chapters_{externalMangaId}_vi";
                string chapterRequestKey = $"chapter_req_{externalMangaId}_vi";
                
                // Lấy danh sách chapter (từ cache hoặc API)
                var chapterDtos = await _mangaDexService.GetMangaChaptersAsync(externalMangaId, "vi");
                
                if (chapterDtos == null || chapterDtos.Count == 0)
                {
                    _logger.LogInformation($"Không có chapter nào cho truyện {externalMangaId}");
                    return;
                }
                
                _logger.LogInformation($"Đã lấy {chapterDtos.Count} chapter cho truyện {externalMangaId}");
                
                // Lấy danh sách chapter hiện có trong database
                var existingChapters = await _context.Chapters
                    .Where(c => c.MangaId == internalMangaId)
                    .ToListAsync();
                
                // Tạo dictionary để tra cứu nhanh - với xử lý trùng lặp
                var existingChaptersDict = new Dictionary<string, Chapter>();
                foreach (var chapter in existingChapters)
                {
                    if (!string.IsNullOrEmpty(chapter.ExternalId) && !existingChaptersDict.ContainsKey(chapter.ExternalId))
                    {
                        existingChaptersDict.Add(chapter.ExternalId, chapter);
                    }
                    else if (!string.IsNullOrEmpty(chapter.ExternalId))
                    {
                        _logger.LogWarning($"Phát hiện chapter trùng lặp với ExternalId {chapter.ExternalId} (ChapterId: {chapter.ChapterId}). Chỉ sử dụng một bản ghi.");
                    }
                }
                
                int totalChapters = chapterDtos.Count;
                int processedCount = 0;
                int successCount = 0;
                int skippedCount = 0;
                int updatedCount = 0;
                int newCount = 0;
                int rateLimitHits = 0;
                int cacheHitCount = 0;
                
                // Giảm số lượng chapter đồng bộ cùng lúc để tránh vượt quá rate limit
                const int batchSize = 5;
                
                // Xử lý tuần tự thay vì song song để tránh lỗi DbContext
                for (int i = 0; i < chapterDtos.Count; i += batchSize)
                {
                    var batch = chapterDtos.Skip(i).Take(batchSize).ToList();
                    _logger.LogInformation($"Đồng bộ batch {i / batchSize + 1}/{(chapterDtos.Count + batchSize - 1) / batchSize}, {batch.Count} chapter");
                    
                    foreach (var chapterDto in batch)
                    {
                        try
                        {
                            processedCount++;
                            
                            // Kiểm tra xem chapter đã tồn tại chưa
                            if (existingChaptersDict.TryGetValue(chapterDto.Id, out var existingChapter))
                            {
                                // Chapter đã tồn tại, kiểm tra ngày cập nhật
                                var chapterUpdatedAt = chapterDto.Attributes.PublishAt;
                                
                                // Nếu ngày cập nhật không thay đổi, bỏ qua chapter này
                                if (existingChapter.UploadDate == chapterUpdatedAt)
                                {
                                    _logger.LogDebug($"Bỏ qua chapter {chapterDto.Attributes.Chapter} (ID: {chapterDto.Id}) vì không có cập nhật mới");
                                    skippedCount++;
                                    successCount++;
                                    continue;
                                }
                                
                                _logger.LogInformation($"Cập nhật chapter {chapterDto.Attributes.Chapter} (ID: {chapterDto.Id}) do có thay đổi");
                                updatedCount++;
                            }
                            else
                            {
                                _logger.LogInformation($"Thêm mới chapter {chapterDto.Attributes.Chapter} (ID: {chapterDto.Id})");
                                newCount++;
                            }
                            
                            // Thêm hoặc cập nhật chapter
                            var chapter = await _chapterRepository.AddOrUpdateChapterAsync(chapterDto, internalMangaId, _mangadexSourceId);
                            
                            // Kiểm tra nếu cần tải nội dung chapter
                            bool needContent = !existingChaptersDict.ContainsKey(chapterDto.Id) || 
                                             !await _context.Pages.AnyAsync(p => p.ChapterId == (existingChaptersDict.ContainsKey(chapterDto.Id) ? existingChaptersDict[chapterDto.Id].ChapterId : 0)) ||
                                             (existingChaptersDict.ContainsKey(chapterDto.Id) && existingChaptersDict[chapterDto.Id].UploadDate != chapterDto.Attributes.PublishAt) ||
                                             await _context.Pages.AnyAsync(p => p.ChapterId == existingChaptersDict[chapterDto.Id].ChapterId && p.UpdatedAt < DateTime.Now.AddDays(-30));
                            
                            if (chapter != null && needContent)
                            {
                                // Kiểm tra nhanh xem cache nội dung chapter có tồn tại
                                string chapterContentCacheKey = $"chapter_content_{chapterDto.Id}";
                                bool hasContentCache = await _apiCacheService.CacheExistsAsync(chapterContentCacheKey);
                                
                                if (hasContentCache)
                                {
                                    cacheHitCount++;
                                }
                                
                                // Lấy nội dung chapter (từ cache hoặc API)
                                var chapterContent = await _mangaDexService.GetChapterContentAsync(chapterDto.Id);
                                
                                // Thêm pages cho chapter nếu có nội dung
                                if (chapterContent != null && chapterContent.Chapter != null &&
                                    chapterContent.Chapter.Data != null && chapterContent.Chapter.Data.Count > 0)
                                {
                                    try {
                                        await _chapterRepository.AddChapterPagesAsync(chapter.ChapterId, chapterContent);
                                        _logger.LogInformation($"Đã thêm {chapterContent.Chapter.Data.Count} trang cho chapter {chapterDto.Id}");
                                        
                                        // Thêm delay ngắn sau khi tải nội dung chapter để tránh rate limit từ at-home server
                                        await Task.Delay(1500);
                                    }
                                    catch (Exception ex) {
                                        if (ex.Message.Contains("rate limit") || ex.Message.Contains("429")) {
                                            _logger.LogWarning($"Gặp rate limit khi tải nội dung chapter {chapterDto.Id}. Tạm dừng 5 giây.");
                                            rateLimitHits++;
                                            await Task.Delay(5000);
                                        }
                                        else {
                                            throw;
                                        }
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning($"Không thể lấy nội dung cho chapter {chapterDto.Id}");
                                }
                            }
                            
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains("rate limit") || ex.Message.Contains("429"))
                            {
                                rateLimitHits++;
                            }
                            _logger.LogError(ex, $"Lỗi khi lưu chapter {chapterDto.Id} cho truyện {externalMangaId}");
                        }
                    }
                    
                    // Thêm delay nghỉ giữa các batch để tránh rate limit
                    if (i + batchSize < chapterDtos.Count)
                    {
                        var delayTime = rateLimitHits > 0 ? 5000 : 1000;
                        _logger.LogInformation($"Tạm dừng {delayTime/1000} giây giữa các batch chapters...");
                        await Task.Delay(delayTime);
                        rateLimitHits = 0; // Reset counter
                    }
                    
                    // Log tiến độ
                    _logger.LogInformation($"Tiến độ đồng bộ: {processedCount}/{totalChapters} chapters ({(processedCount * 100 / totalChapters)}%)");
                }
                
                try
                {
                    // Cập nhật thông tin truyện sau khi đồng bộ chapter
                    manga = await _mangaRepository.GetByIdAsync(internalMangaId);
                    if (manga != null)
                    {
                        // Set chapter count
                        manga.ChapterCount = chapterDtos.Count;
                        
                        // Cập nhật LastSyncAt 
                        manga.LastSyncAt = DateTime.Now;
                        
                        // Cập nhật UpdatedAt nếu có mangaUpdatedAt từ API
                        manga.UpdatedAt = mangaUpdatedAt;
                        
                        // Lưu thay đổi
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Lỗi khi cập nhật thông tin truyện {internalMangaId} sau khi đồng bộ chapter");
                }
                
                _logger.LogInformation($"Đã hoàn thành đồng bộ chapter cho truyện {externalMangaId}. " +
                                     $"Thành công: {successCount}/{chapterDtos.Count} chapter " +
                                     $"(Mới: {newCount}, Cập nhật: {updatedCount}, Bỏ qua: {skippedCount}, Cache hit: {cacheHitCount})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi đồng bộ chapter cho truyện {externalMangaId}");
            }
        }
        
        private async Task UpdateMangaDatesFromChaptersAsync(int mangaId)
        {
            try
            {
                var chapters = await _context.Chapters
                    .Where(c => c.MangaId == mangaId)
                    .OrderBy(c => c.UploadDate)
                    .ToListAsync();

                if (chapters != null && chapters.Any())
                {
                    var manga = await _context.Mangas.FindAsync(mangaId);
                    if (manga != null)
                    {
                        // Đảm bảo các ngày được chuyển đổi đúng cách từ API MangaDex
                        // Lấy ngày của chapter đầu tiên (cũ nhất) làm ngày tạo manga
                        manga.CreatedAt = chapters.First().UploadDate;
                        
                        // Lấy ngày của chapter mới nhất làm ngày cập nhật manga
                        manga.UpdatedAt = chapters.OrderByDescending(c => c.UploadDate).First().UploadDate;
                        
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Updated manga dates for manga ID {mangaId}. Created: {manga.CreatedAt}, Updated: {manga.UpdatedAt}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating manga dates for manga ID {mangaId}");
            }
        }
        
        private string ExtractMangaDexIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;
                
            // Trích xuất UUID từ URL MangaDex (định dạng: /title/12345678-1234-1234-1234-123456789abc)
            var regex = new Regex(@"title/([a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12})", RegexOptions.IgnoreCase);
            var match = regex.Match(url);
            
            if (match.Success && match.Groups.Count > 1)
                return match.Groups[1].Value;
                
            return null;
        }
    }
}