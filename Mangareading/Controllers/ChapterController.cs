using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mangareading.Services;
using Mangareading.Repositories;
using Mangareading.Models;
using Microsoft.EntityFrameworkCore;

namespace Mangareading.Controllers
{
    [Route("chapter")]
    public class ChapterController : Controller
    {
        private readonly MangaDexService _mangaDexService;
        private readonly IChapterRepository _chapterRepository;
        private readonly IMangaRepository _mangaRepository;
        private readonly ILogger<ChapterController> _logger;
        private readonly int _mangadexSourceId = 1; // ID MangaDex trong Sources
        private readonly IViewCountRepository _viewCountRepository;
        private readonly IReadingHistoryRepository _readingHistoryRepository;
        private readonly ICommentRepository _commentRepository;
        private readonly YourDbContext _context;

        public ChapterController(
            MangaDexService mangaDexService,
            IChapterRepository chapterRepository,
            IMangaRepository mangaRepository,
            ILogger<ChapterController> logger,
            IViewCountRepository viewCountRepository,
            IReadingHistoryRepository readingHistoryRepository,
            ICommentRepository commentRepository,
            YourDbContext context)
        {
            _mangaDexService = mangaDexService;
            _chapterRepository = chapterRepository;
            _mangaRepository = mangaRepository;
            _logger = logger;
            _viewCountRepository = viewCountRepository;
            _readingHistoryRepository = readingHistoryRepository;
            _commentRepository = commentRepository;
            _context = context;
        }

        [HttpGet("list/{mangaId:int}")]
        public async Task<IActionResult> ListChapters(int mangaId, int page = 1)
        {
            try
            {
                var manga = await _mangaRepository.GetByIdAsync(mangaId);
                if (manga == null)
                {
                    return NotFound("Không tìm thấy truyện");
                }

                // Đếm số chapter trước khi đồng bộ
                var existingChapters = await _chapterRepository.GetChaptersByMangaIdAsync(mangaId);
                int existingChapterCount = existingChapters?.Count ?? 0;

                // Luôn đồng bộ chapter mới từ API bất kể có sẵn chapter trong DB hay không
                if (!string.IsNullOrEmpty(manga.ExternalId))
                {
                    await SyncChaptersFromApi(manga);

                    // Lấy lại danh sách sau khi đã đồng bộ
                    var updatedChapters = await _chapterRepository.GetChaptersByMangaIdAsync(mangaId);

                    // Tính số chapter mới
                    int newChapterCount = (updatedChapters?.Count ?? 0) - existingChapterCount;
                    ViewBag.NewChapterCount = newChapterCount;

                    existingChapters = updatedChapters;
                }
                else if (existingChapterCount == 0)
                {
                    return View("NoChapters", manga);
                }

                // Thiết lập phân trang
                ViewBag.CurrentPage = page;
                ViewBag.Manga = manga;

                // Hiện tại của người dùng
                ViewBag.CurrentDateTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                ViewBag.CurrentUser = User.Identity.Name;

                return View(existingChapters ?? new List<Chapter>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting chapters for manga {mangaId}");
                return View("Error", new ErrorViewModel
                {
                    RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    Message = "Không thể tải danh sách chapter. Vui lòng thử lại sau."
                });
            }
        }

        // Xem nội dung chapter
        [HttpGet("read/{chapterId:int}")]
        public async Task<IActionResult> Read(int chapterId)
        {
            try
            {
                var chapter = await _chapterRepository.GetChapterByIdAsync(chapterId);
                if (chapter == null)
                {
                    return NotFound("Không tìm thấy chapter");
                }

                var pages = await _chapterRepository.GetChapterPagesAsync(chapterId);

                // Nếu chưa có page nào trong DB, gọi API để lấy
                if (pages == null || pages.Count == 0)
                {
                    if (string.IsNullOrEmpty(chapter.ExternalId))
                    {
                        return View("NoContent", chapter);
                    }

                    await SyncChapterContentFromApi(chapter);

                    // Lấy lại danh sách sau khi đã đồng bộ
                    pages = await _chapterRepository.GetChapterPagesAsync(chapterId);
                }

                // Get manga title for breadcrumb
                var manga = await _mangaRepository.GetByIdAsync(chapter.MangaId.Value);

                ViewBag.Chapter = chapter;
                ViewBag.Manga = manga;

                // Get previous and next chapters
                var allChapters = await _chapterRepository.GetChaptersByMangaIdAsync(chapter.MangaId.Value);
                var orderedChapters = allChapters.OrderBy(c => c.ChapterNumber).ToList();
                var currentIndex = orderedChapters.FindIndex(c => c.ChapterId == chapter.ChapterId);

                if (currentIndex > 0)
                {
                    ViewBag.PrevChapter = orderedChapters[currentIndex - 1];
                }

                if (currentIndex < orderedChapters.Count - 1)
                {
                    ViewBag.NextChapter = orderedChapters[currentIndex + 1];
                }

                // Lấy IP của người dùng để ghi nhận lượt xem duy nhất
                string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                // Ghi nhận lượt xem
                try
                {
                    // Lấy thông tin user nếu đã đăng nhập
                    int? userId = null;
                    if (User.Identity.IsAuthenticated)
                    {
                        var userIdClaim = User.Claims.FirstOrDefault(c =>
                            c.Type == "UserId" || c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);

                        if (userIdClaim != null && !string.IsNullOrEmpty(userIdClaim.Value) && int.TryParse(userIdClaim.Value, out int id))
                        {
                            userId = id;

                            // Thêm vào lịch sử đọc truyện
                            await _readingHistoryRepository.AddToHistoryAsync(userId.Value, chapter.MangaId.Value, chapterId);
                        }
                    }

                    // Thêm lượt xem
                    await _viewCountRepository.AddViewAsync(chapter.MangaId.Value, chapterId, userId, ipAddress);
                }
                catch (Exception ex)
                {
                    // Log lỗi nhưng không làm ảnh hưởng đến việc hiển thị trang
                    _logger.LogError(ex, "Error recording view or reading history");
                }

                return View(pages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reading chapter {chapterId}");
                return View("Error", new ErrorViewModel
                {
                    RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    Message = "Không thể tải nội dung chapter. Vui lòng thử lại sau."
                });
            }
        }

        // API để đồng bộ danh sách chapter
        [HttpPost("sync/{mangaId:int}")]
        public async Task<IActionResult> SyncChapters(int mangaId)
        {
            try
            {
                var manga = await _mangaRepository.GetByIdAsync(mangaId);
                if (manga == null)
                {
                    return NotFound("Không tìm thấy truyện");
                }

                await SyncChaptersFromApi(manga);

                return RedirectToAction("ListChapters", new { mangaId = mangaId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error syncing chapters for manga {mangaId}");
                return View("Error", new ErrorViewModel
                {
                    RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    Message = "Không thể đồng bộ chapter. Vui lòng thử lại sau."
                });
            }
        }

        // Helper method to sync chapters from API
        private async Task SyncChaptersFromApi(Manga manga)
        {
            var chapterDtos = await _mangaDexService.GetMangaChaptersAsync(manga.ExternalId);

            _logger.LogInformation($"Found {chapterDtos.Count} chapters for manga {manga.MangaId} from API");

            foreach (var chapterDto in chapterDtos)
            {
                try
                {
                    await _chapterRepository.AddOrUpdateChapterAsync(
                        chapterDto, manga.MangaId, _mangadexSourceId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error syncing chapter {chapterDto.Id}");
                }
            }
        }

        // Helper method to sync chapter content from API
        private async Task SyncChapterContentFromApi(Chapter chapter)
        {
            var chapterContent = await _mangaDexService.GetChapterContentAsync(chapter.ExternalId);

            if (chapterContent == null)
            {
                _logger.LogWarning($"No content found for chapter {chapter.ChapterId}");
                return;
            }

            // Check if pages already exist
            var existingPages = await _context.Pages
                .Where(p => p.ChapterId == chapter.ChapterId)
                .ToListAsync();

            if (existingPages.Any())
            {
                // Update existing pages' UpdatedAt timestamp
                foreach (var page in existingPages)
                {
                    page.UpdatedAt = DateTime.Now;
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Updated timestamps for {existingPages.Count} existing pages for chapter {chapter.ChapterId}");
            }
            else
            {
                // Add new pages
                await _chapterRepository.AddChapterPagesAsync(chapter.ChapterId, chapterContent);
            }
        }
    }
}
