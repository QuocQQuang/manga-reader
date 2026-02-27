using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mangareading.Models;
using Mangareading.DTOs;

namespace Mangareading.Repositories
{
    public class ChapterRepository : IChapterRepository
    {
        private readonly YourDbContext _context;
        private readonly ILogger<ChapterRepository> _logger;

        public ChapterRepository(YourDbContext context, ILogger<ChapterRepository> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<List<Chapter>> GetChaptersByMangaIdAsync(int mangaId)
        {
            try
            {
                // Xây dựng truy vấn với hàm Select để xử lý các giá trị null
                return await _context.Chapters
                    .Where(c => c.MangaId == mangaId)
                    .Select(c => new Chapter
                    {
                        ChapterId = c.ChapterId,
                        MangaId = c.MangaId,
                        Title = c.Title ?? string.Empty,
                        ChapterNumber = c.ChapterNumber,
                        LanguageCode = c.LanguageCode ?? string.Empty,
                        SourceId = c.SourceId,
                        ExternalId = c.ExternalId ?? string.Empty,
                        UploadDate = c.UploadDate
                    })
                    .OrderByDescending(c => c.ChapterNumber)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting chapters for manga {mangaId}");

                // Trả về danh sách trống thay vì ném ngoại lệ
                return new List<Chapter>();
            }
        }

        public async Task<Chapter> GetChapterByExternalIdAsync(string externalId)
        {
            try
            {
                return await _context.Chapters
                    .Select(c => new Chapter
                    {
                        ChapterId = c.ChapterId,
                        MangaId = c.MangaId,
                        Title = c.Title ?? string.Empty,
                        ChapterNumber = c.ChapterNumber,
                        LanguageCode = c.LanguageCode ?? string.Empty,
                        SourceId = c.SourceId,
                        ExternalId = c.ExternalId ?? string.Empty,
                        UploadDate = c.UploadDate
                    })
                    .FirstOrDefaultAsync(c => c.ExternalId == externalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting chapter by external ID {externalId}");
                return null;
            }
        }

        public async Task<Chapter> GetChapterByIdAsync(int chapterId)
        {
            try
            {
                return await _context.Chapters
                    .Select(c => new Chapter
                    {
                        ChapterId = c.ChapterId,
                        MangaId = c.MangaId,
                        Title = c.Title ?? string.Empty,
                        ChapterNumber = c.ChapterNumber,
                        LanguageCode = c.LanguageCode ?? string.Empty,
                        SourceId = c.SourceId,
                        ExternalId = c.ExternalId ?? string.Empty,
                        UploadDate = c.UploadDate
                    })
                    .FirstOrDefaultAsync(c => c.ChapterId == chapterId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting chapter by ID {chapterId}");
                return null;
            }
        }

        public async Task<bool> ChapterExistsAsync(string externalId)
        {
            return await _context.Chapters.AnyAsync(c => c.ExternalId == externalId);
        }

        public async Task<Chapter> AddOrUpdateChapterAsync(ChapterDto chapterDto, int mangaId, int sourceId)
        {
            try
            {
                var existingChapter = await _context.Chapters
                    .FirstOrDefaultAsync(c => c.ExternalId == chapterDto.Id);

                // Xử lý ngày tháng an toàn để tránh lỗi
                DateTime validPublishDate;
                try
                {
                    // PublishAt được định nghĩa là DateTime nên lấy trực tiếp
                    validPublishDate = chapterDto.Attributes.PublishAt;

                    // Đảm bảo chuyển đổi từ UTC sang giờ địa phương nếu cần
                    if (validPublishDate.Kind == DateTimeKind.Utc)
                    {
                        validPublishDate = DateTime.SpecifyKind(validPublishDate, DateTimeKind.Utc);
                    }

                    // Kiểm tra tính hợp lệ của ngày
                    if (validPublishDate.Year > DateTime.Now.Year + 1 || validPublishDate.Year < 2000)
                    {
                        _logger.LogWarning($"Chapter {chapterDto.Id} has invalid date {validPublishDate}. Using current date instead.");
                        validPublishDate = DateTime.Now;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error parsing date for chapter {chapterDto.Id}. Using current date.");
                    validPublishDate = DateTime.Now;
                }

                _logger.LogInformation($"Chapter {chapterDto.Id} publish date: {validPublishDate} (Type: {validPublishDate.Kind})");

                if (existingChapter == null)
                {
                    // Tạo chapter mới
                    existingChapter = new Chapter
                    {
                        MangaId = mangaId,
                        Title = chapterDto.Attributes.Title,
                        ChapterNumber = decimal.TryParse(chapterDto.Attributes.Chapter, out decimal chNumber) ? chNumber : 0,
                        LanguageCode = chapterDto.Attributes.TranslatedLanguage,
                        SourceId = sourceId,
                        ExternalId = chapterDto.Id,
                        UploadDate = validPublishDate
                    };

                    _context.Chapters.Add(existingChapter);
                }
                else
                {
                    // Cập nhật thông tin chapter
                    existingChapter.Title = chapterDto.Attributes.Title;
                    existingChapter.ChapterNumber = decimal.TryParse(chapterDto.Attributes.Chapter, out decimal chNumber) ? chNumber : existingChapter.ChapterNumber;
                    existingChapter.LanguageCode = chapterDto.Attributes.TranslatedLanguage;
                    existingChapter.UploadDate = validPublishDate;
                }

                await _context.SaveChangesAsync();
                return existingChapter;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving chapter {chapterDto.Id}");
                throw;
            }
        }

        public async Task<List<Page>> GetChapterPagesAsync(int chapterId)
        {
            return await _context.Pages
                .Where(p => p.ChapterId == chapterId)
                .OrderBy(p => p.PageNumber)
                .ToListAsync();
        }

        public async Task AddChapterPagesAsync(int chapterId, ChapterReadDto chapterContent)
        {
            try
            {
                // Kiểm tra xem chapter đã có pages chưa
                var existingPages = await _context.Pages
                    .Where(p => p.ChapterId == chapterId)
                    .ToListAsync();

                if (existingPages.Any())
                {
                    _logger.LogInformation($"Chapter {chapterId} already has {existingPages.Count} pages. Skipping...");
                    return;
                }

                var pages = new List<Page>();
                var imageFiles = chapterContent.Chapter.Data; // Sử dụng data chất lượng cao
                var baseUrl = chapterContent.BaseUrl;
                var hash = chapterContent.Chapter.Hash;

                // Kiểm tra hash và baseUrl hợp lệ
                if (string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(baseUrl))
                {
                    _logger.LogWarning($"Invalid hash or baseUrl for chapter {chapterId}. Hash: {hash}, BaseUrl: {baseUrl}");
                    return;
                }

                for (int i = 0; i < imageFiles.Count; i++)
                {
                    var imageFile = imageFiles[i];

                    // Kiểm tra tên file hợp lệ
                    if (string.IsNullOrWhiteSpace(imageFile))
                    {
                        _logger.LogWarning($"Skipping invalid image file name at index {i} for chapter {chapterId}");
                        continue;
                    }

                    var imageUrl = $"{baseUrl}/data/{hash}/{imageFile}";

                    // Kiểm tra URL cuối cùng hợp lệ
                    if (!Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
                    {
                        _logger.LogWarning($"Skipping malformed URL: {imageUrl} for chapter {chapterId}, page {i+1}");
                        continue;
                    }

                    var page = new Page
                    {
                        ChapterId = chapterId,
                        PageNumber = i + 1,
                        ImageUrl = imageUrl,
                        UpdatedAt = DateTime.Now
                    };

                    pages.Add(page);
                }

                // Chỉ lưu nếu có trang hợp lệ
                if (pages.Count > 0)
                {
                    await _context.Pages.AddRangeAsync(pages);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Added {pages.Count} pages for chapter {chapterId}");
                }
                else
                {
                    _logger.LogWarning($"No valid pages found for chapter {chapterId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding pages for chapter {chapterId}");
                throw;
            }
        }

        // Upload chapter methods implementation
        public async Task<Chapter> CreateChapterAsync(Chapter chapterData)
        {
            try
            {
                // Set upload date
                if (chapterData.UploadDate == default)
                {
                    chapterData.UploadDate = DateTime.Now;
                }

                // Add chapter to database
                _context.Chapters.Add(chapterData);
                await _context.SaveChangesAsync();

                return chapterData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating chapter for manga {chapterData.MangaId}");
                throw;
            }
        }

        public async Task<Chapter> UpdateChapterAsync(int chapterId, Chapter chapterData)
        {
            try
            {
                var existingChapter = await _context.Chapters.FindAsync(chapterId);

                if (existingChapter == null)
                {
                    throw new KeyNotFoundException($"Chapter with ID {chapterId} not found");
                }

                // Update basic properties
                existingChapter.Title = chapterData.Title;
                existingChapter.ChapterNumber = chapterData.ChapterNumber;
                existingChapter.LanguageCode = chapterData.LanguageCode;

                await _context.SaveChangesAsync();
                return existingChapter;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating chapter with ID {chapterId}");
                throw;
            }
        }

        public async Task DeleteChapterAsync(int chapterId)
        {
            try
            {
                var chapter = await _context.Chapters
                    .Include(c => c.Pages)
                    .FirstOrDefaultAsync(c => c.ChapterId == chapterId);

                if (chapter == null)
                {
                    throw new KeyNotFoundException($"Chapter with ID {chapterId} not found");
                }

                // Delete pages first
                _context.Pages.RemoveRange(chapter.Pages);

                // Delete comments
                var comments = await _context.Comments
                    .Where(c => c.ChapterId == chapterId)
                    .ToListAsync();
                _context.Comments.RemoveRange(comments);

                // Delete chapter
                _context.Chapters.Remove(chapter);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting chapter with ID {chapterId}");
                throw;
            }
        }

        public async Task UpdateChapterPagesAsync(int chapterId, List<string> orderedImageUrls)
        {
            try
            {
                // Verify chapter exists
                var chapter = await _context.Chapters.FindAsync(chapterId);
                if (chapter == null)
                {
                    throw new KeyNotFoundException($"Chapter with ID {chapterId} not found");
                }

                // Delete existing pages
                var existingPages = await _context.Pages
                    .Where(p => p.ChapterId == chapterId)
                    .ToListAsync();
                _context.Pages.RemoveRange(existingPages);

                // Add new pages with correct order
                var newPages = new List<Page>();
                for (int i = 0; i < orderedImageUrls.Count; i++)
                {
                    var imageUrl = orderedImageUrls[i];
                    if (!string.IsNullOrWhiteSpace(imageUrl))
                    {
                        newPages.Add(new Page
                        {
                            ChapterId = chapterId,
                            PageNumber = i + 1,
                            ImageUrl = imageUrl,
                            UpdatedAt = DateTime.Now
                        });
                    }
                }

                if (newPages.Any())
                {
                    await _context.Pages.AddRangeAsync(newPages);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Updated {newPages.Count} pages for chapter {chapterId}");
                }
                else
                {
                    _logger.LogWarning($"No valid pages provided for chapter {chapterId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating pages for chapter {chapterId}");
                throw;
            }
        }

        public async Task<List<Page>> GetPagesByChapterIdAsync(int chapterId)
        {
            try
            {
                return await _context.Pages
                    .Where(p => p.ChapterId == chapterId)
                    .OrderBy(p => p.PageNumber)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting pages for chapter {chapterId}");
                return new List<Page>();
            }
        }
    }
}