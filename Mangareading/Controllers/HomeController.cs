using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mangareading.Models;
using Mangareading.Repositories;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System;

namespace Mangareading.Controllers
{
    public class HomeController : Controller
    {
        private readonly YourDbContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly ICommentRepository _commentRepository;
        private readonly IChapterRepository _chapterRepository;

        public HomeController(
            YourDbContext context,
            ILogger<HomeController> logger,
            ICommentRepository commentRepository,
            IChapterRepository chapterRepository)
        {
            _context = context;
            _logger = logger;
            _commentRepository = commentRepository;
            _chapterRepository = chapterRepository;
        }

        public async Task<IActionResult> Index(int page = 1, string sort = "latest")
        {
            try
            {
                // Ensure page is valid
                if (page < 1) page = 1;

                int pageSize = 12; // Number of manga to display per page

                // Build the base query with filtering and sorting
                var query = _context.Mangas
                    .Include(m => m.MangaGenres)
                    .ThenInclude(mg => mg.Genre)
                    .AsSplitQuery()
                    .AsQueryable();

                // Apply sorting
                switch (sort)
                {
                    case "views":
                        query = query.OrderByDescending(m => m.ViewCount);
                        break;
                    case "newest":
                        query = query.OrderByDescending(m => m.CreatedAt);
                        break;
                    case "latest":
                    default:
                        query = query.OrderByDescending(m => m.UpdatedAt);
                        break;
                }

                // Get total count for pagination
                var count = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(count / (double)pageSize);

                // Ensure page doesn't exceed totalPages
                if (page > totalPages && totalPages > 0)
                {
                    page = totalPages;
                }

                // Get paginated manga list
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

                // Lấy 5 bình luận mới nhất
                List<Comment> latestComments = new List<Comment>();
                try
                {
                    latestComments = await _commentRepository.GetLatestCommentsAsync(5); // Assume GetLatestCommentsAsync exists
                }
                catch (Exception exComment)
                {
                    _logger.LogError(exComment, "Error loading latest comments");
                    // Continue without comments if error occurs
                }

                // Pass data to the view
                ViewBag.LatestChapters = latestChaptersDict;
                ViewBag.LatestComments = latestComments;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalItems = count;
                ViewBag.PageSize = pageSize;
                ViewBag.CurrentSort = sort;

                return View(mangas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading home page manga list");
                // Provide fallback with empty data
                ViewBag.LatestChapters = new Dictionary<int, List<Chapter>>();
                ViewBag.LatestComments = new List<Comment>();
                ViewBag.CurrentPage = 1;
                ViewBag.TotalPages = 1;
                ViewBag.TotalItems = 0;
                ViewBag.PageSize = 12;
                ViewBag.CurrentSort = sort;
                ViewBag.ErrorMessage = "Lỗi khi tải danh sách truyện. Vui lòng thử lại sau.";

                return View(new List<Manga>());
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult TestComment()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult StatusCode(int statusCode, string returnUrl = null)
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            // Lưu returnUrl để sử dụng trong các view
            if (!string.IsNullOrEmpty(returnUrl))
            {
                ViewBag.ReturnUrl = returnUrl;
            }

            switch (statusCode)
            {
                case 400:
                    return View("BadRequest", new ErrorViewModel
                    {
                        RequestId = requestId,
                        Message = "Yêu cầu của bạn không hợp lệ.",
                        StatusCode = 400
                    });

                case 401:
                    // Thêm nút đăng nhập và thông báo rõ ràng hơn
                    return View("Unauthorized", new ErrorViewModel
                    {
                        RequestId = requestId,
                        Message = "Bạn cần đăng nhập để truy cập trang này. Vui lòng đăng nhập và thử lại.",
                        StatusCode = 401
                    });

                case 403:
                    // Thông báo rõ ràng hơn về quyền truy cập
                    return View("Forbidden", new ErrorViewModel
                    {
                        RequestId = requestId,
                        Message = "Tài khoản của bạn không có đủ quyền để truy cập trang này. Vui lòng liên hệ quản trị viên nếu bạn cần hỗ trợ.",
                        StatusCode = 403
                    });

                case 404:
                    return View("NotFound", new ErrorViewModel
                    {
                        RequestId = requestId,
                        Message = "Trang bạn đang tìm kiếm không tồn tại.",
                        StatusCode = 404
                    });

                case 500:
                    return View("ServerError", new ErrorViewModel
                    {
                        RequestId = requestId,
                        Message = "Đã xảy ra lỗi trên máy chủ khi xử lý yêu cầu của bạn.",
                        StatusCode = 500
                 });

                case 503:
                    return View("ServiceUnavailable", new ErrorViewModel
                    {
                        RequestId = requestId,
                        Message = "Dịch vụ tạm thời không khả dụng. Vui lòng thử lại sau.",
                        StatusCode = 503
                    });

                default:
                    // For any other status code, use the general error view
                    return View("Error", new ErrorViewModel
                    {
                        RequestId = requestId,
                        Message = $"Đã xảy ra lỗi với mã trạng thái {statusCode}.",
                        StatusCode = statusCode
                    });
            }
        }
        [HttpGet("serverip")]
        public IActionResult ServerIp()
        {
            var ipAddresses = GetLocalIPAddresses();
            return Content($"Server IP Addresses:\n{string.Join("\n", ipAddresses)}");
            _logger.LogInformation($"Server IP Addresses:\n{string.Join("\n", ipAddresses)}");
        }

        private List<string> GetLocalIPAddresses()
        {
            var hostName = Dns.GetHostName();
            var ips = Dns.GetHostEntry(hostName).AddressList
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .Select(ip => ip.ToString())
                .ToList();

            return ips;
        }
    }
}