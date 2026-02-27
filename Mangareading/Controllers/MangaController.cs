using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mangareading.Models;
using Mangareading.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System;

using Mangareading.Repositories;


namespace Mangareading.Controllers
{
    [Route("manga")]
    public class MangaController : Controller
    {
        private readonly YourDbContext _context;
        private readonly ILogger<MangaController> _logger;
        private readonly IChapterRepository _chapterRepository;
        private readonly MangaDexService _mangaDexService;
        private readonly IReadingHistoryRepository _readingHistoryRepository;
        private readonly IViewCountRepository _viewCountRepository;
        private readonly ICommentRepository _commentRepository;
        private readonly MangaSyncService _mangaSyncService;

        public MangaController(
            YourDbContext context,
            ILogger<MangaController> logger,
            IChapterRepository chapterRepository,
            MangaDexService mangaDexService,
            IReadingHistoryRepository readingHistoryRepository,
            IViewCountRepository viewCountRepository,
            ICommentRepository commentRepository,
            MangaSyncService mangaSyncService)
        {
            _context = context;
            _logger = logger;
            _chapterRepository = chapterRepository;
            _mangaDexService = mangaDexService;
            _readingHistoryRepository = readingHistoryRepository;
            _viewCountRepository = viewCountRepository;
            _commentRepository = commentRepository;
            _mangaSyncService = mangaSyncService;
        }


        [HttpGet("")]
        public async Task<IActionResult> Index(
            int page = 1, 
            string sort = "latest", 
            string search = "",
            int? genreId = null,
            string author = "",
            string status = "")
        {
            try
            {
                // Ensure page is valid
                if (page < 1) page = 1;
                
                int pageSize = 20;

                var query = _context.Mangas
                    .Include(m => m.MangaGenres)
                    .ThenInclude(mg => mg.Genre)
                    .AsSplitQuery()
                    .AsQueryable();
                
                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(m => 
                        m.Title.Contains(search) || 
                        (m.AlternativeTitle != null && m.AlternativeTitle.Contains(search)) ||
                        (m.Description != null && m.Description.Contains(search)));
                }
                
                // Apply genre filter
                if (genreId.HasValue)
                {
                    query = query.Where(m => m.MangaGenres.Any(mg => mg.GenreId == genreId.Value));
                }
                
                // Apply author filter
                if (!string.IsNullOrEmpty(author))
                {
                    query = query.Where(m => m.Author != null && m.Author.Contains(author));
                }
                
                // Apply status filter
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(m => m.Status == status);
                }
                
                // Apply sorting
                switch (sort)
                {
                    case "az":
                        query = query.OrderBy(m => m.Title);
                        break;
                    case "za":
                        query = query.OrderByDescending(m => m.Title);
                        break;
                    case "views":
                        query = query.OrderByDescending(m => m.ViewCount);
                        break;
                    case "newest":
                        query = query.OrderByDescending(m => m.CreatedAt);
                        break;
                    case "oldest":
                        query = query.OrderBy(m => m.CreatedAt);
                        break;
                    case "latest":
                    default:
                        query = query.OrderByDescending(m => m.UpdatedAt);
                        break;
                }

                var count = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(count / (double)pageSize);
                
                // Ensure page doesn't exceed totalPages
                if (page > totalPages && totalPages > 0)
                {
                    page = totalPages;
                }
                
                var mangas = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Lấy danh sách chapter mới nhất cho mỗi manga (1 query thay vì N queries)
                var mangaIds = mangas.Select(m => m.MangaId).ToList();
                var allChapters = await _context.Chapters
                    .Where(c => c.MangaId != null && mangaIds.Contains(c.MangaId.Value))
                    .OrderByDescending(c => c.ChapterNumber)
                    .ToListAsync();
                var latestChaptersDict = allChapters
                    .GroupBy(c => c.MangaId!.Value)
                    .ToDictionary(g => g.Key, g => g.Take(3).ToList());

                // Get all genres for the filter buttons
                List<Genre> genres = new List<Genre>();
                try
                {
                    genres = await _context.Genres.OrderBy(g => g.GenreName).ToListAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading genres");
                    // Ensure we still have an empty list rather than null
                    genres = new List<Genre>();
                }

                ViewBag.LatestChapters = latestChaptersDict;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalItems = count;
                ViewBag.PageSize = pageSize;
                ViewBag.CurrentSort = sort;
                ViewBag.CurrentSearch = search;
                ViewBag.CurrentGenreId = genreId;
                ViewBag.CurrentAuthor = author;
                ViewBag.CurrentStatus = status;
                ViewBag.Genres = genres; // Always set, even if empty

                return View(mangas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading manga list: {ex.Message}");
                
                // Provide a fallback with empty data to avoid runtime errors
                ViewBag.LatestChapters = new Dictionary<int, List<Chapter>>();
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = 1;
                ViewBag.TotalItems = 0;
                ViewBag.PageSize = 20;
                ViewBag.CurrentSort = sort;
                ViewBag.CurrentSearch = search;
                ViewBag.CurrentGenreId = genreId; // This could be null
                ViewBag.CurrentAuthor = author;
                ViewBag.CurrentStatus = status;
                ViewBag.Genres = new List<Genre>(); // Ensure this is never null
                
                // Show empty list with error message
                ViewBag.ErrorMessage = "Lỗi khi tải danh sách truyện. Vui lòng thử lại sau.";
                return View(new List<Manga>());
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var manga = await _context.Mangas
                    .Include(m => m.MangaGenres)
                    .ThenInclude(mg => mg.Genre)
                    .FirstOrDefaultAsync(m => m.MangaId == id);

                if (manga == null)
                {
                    return NotFound();
                }

                // Khởi tạo danh sách rỗng để tránh null
                var chapters = new List<Chapter>();

                try
                {
                    // Lấy danh sách chapter từ database
                    chapters = await _chapterRepository.GetChaptersByMangaIdAsync(id);

                    // Không còn tự động đồng bộ khi không có chapter
                    if (chapters == null || !chapters.Any())
                    {
                        _logger.LogInformation($"No chapters found in database for manga {id}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error loading chapters for manga {id}");
                    // Tiếp tục với chapters rỗng thay vì làm gián đoạn trang
                }

                // Nếu người dùng đã đăng nhập, lấy thông tin chapter đọc gần nhất
                if (User.Identity.IsAuthenticated)
                {
                    try
                    {
                        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId" || c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
                        if (userIdClaim != null && !string.IsNullOrEmpty(userIdClaim.Value) && int.TryParse(userIdClaim.Value, out int userId))
                        {
                            var lastReadChapter = await _readingHistoryRepository.GetLastReadChapterAsync(userId, id);
                            ViewBag.LastReadChapter = lastReadChapter;
                        }
                        else
                        {
                            ViewBag.LastReadChapter = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error loading reading history for user and manga {id}");
                    }
                }

                ViewBag.Chapters = chapters;
                return View(manga);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading manga details {id}");
                return View("Error", new ErrorViewModel
                {
                    RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    Message = "Không thể tải thông tin truyện. Vui lòng thử lại sau."
                });
            }
        }


    }
}
