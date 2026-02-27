using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Mangareading.Services;
using Mangareading.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.Json;
using Mangareading.Services.Interfaces;
using System.Collections.Generic;
using Mangareading.Repositories;

namespace Mangareading.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly MangaSyncService _syncService;
        private readonly ILogger<AdminController> _logger;
        private readonly YourDbContext _dbContext;
        private readonly IStatsService _statsService;
        private readonly IMangaRepository _mangaRepository;
        private readonly IChapterRepository _chapterRepository;

        public AdminController(
            MangaSyncService syncService,
            ILogger<AdminController> logger,
            YourDbContext dbContext,
            IStatsService statsService,
            IMangaRepository mangaRepository,
            IChapterRepository chapterRepository)
        {
            _syncService = syncService;
            _logger = logger;
            _dbContext = dbContext;
            _statsService = statsService;
            _mangaRepository = mangaRepository;
            _chapterRepository = chapterRepository;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Collect basic statistics
                var totalUsers = await _dbContext.Users.CountAsync();
                var totalManga = await _dbContext.Mangas.CountAsync();
                var totalChapters = await _dbContext.Chapters.CountAsync();
                var totalViews = await _dbContext.Mangas.SumAsync(m => m.ViewCount);

                // Get recent manga updates (10 most recently updated)
                var recentManga = await _dbContext.Mangas
                    .OrderByDescending(m => m.UpdatedAt)
                    .Take(10)
                    .ToListAsync();

                // Get top 5 popular manga based on view count
                var popularManga = await _dbContext.Mangas
                    .OrderByDescending(m => m.ViewCount)
                    .Take(5)
                    .ToListAsync();

                // Get recent users (10 most recently registered)
                var recentUsers = await _dbContext.Users
                    .Where(u => !u.IsAdmin) // Exclude admin users
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                // Pass all data to the view
                ViewBag.TotalUsers = totalUsers;
                ViewBag.TotalManga = totalManga;
                ViewBag.TotalChapters = totalChapters;
                ViewBag.TotalViews = totalViews;
                ViewBag.RecentManga = recentManga;
                ViewBag.PopularManga = popularManga;
                ViewBag.RecentUsers = recentUsers;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin dashboard data");
                return View();
            }
        }

        [HttpGet("users")]
        public async Task<IActionResult> UserManagement()
        {
            try
            {
                var users = await _dbContext.Users
                    .Where(u => !u.IsAdmin) // Exclude admin users
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();

                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users for management");
                return View(new System.Collections.Generic.List<User>());
            }
        }

        [HttpGet("users/{userId}")]
        public async Task<IActionResult> UserDetails(int userId)
        {
            try
            {
                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                {
                    return NotFound();
                }

                if (user.IsAdmin)
                {
                    TempData["Error"] = "Không thể quản lý người dùng Admin khác.";
                    return RedirectToAction("UserManagement");
                }

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user with ID {userId}");
                return RedirectToAction("UserManagement");
            }
        }

        [HttpGet("manga")]
        public async Task<IActionResult> MangaManagement()
        {
            try
            {
                var mangas = await _dbContext.Mangas
                    .OrderByDescending(m => m.UpdatedAt)
                    .ToListAsync();

                return View(mangas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving manga for management");
                return View(new System.Collections.Generic.List<Manga>());
            }
        }

        [HttpGet("manga/{mangaId}")]
        public async Task<IActionResult> MangaDetails(int mangaId)
        {
            try
            {
                var manga = await _dbContext.Mangas
                    .Include(m => m.Chapters)
                    .FirstOrDefaultAsync(m => m.MangaId == mangaId);

                if (manga == null)
                {
                    return NotFound();
                }

                return View(manga);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving manga with ID {mangaId}");
                return RedirectToAction("MangaManagement");
            }
        }

        [HttpPost("manga/delete")]
        public async Task<IActionResult> DeleteManga(int mangaId)
        {
            try
            {
                _logger.LogInformation($"Attempting to delete manga with ID {mangaId}");

                // Get manga details for logging
                var manga = await _dbContext.Mangas
                    .FirstOrDefaultAsync(m => m.MangaId == mangaId);

                if (manga == null)
                {
                    _logger.LogWarning($"Manga with ID {mangaId} not found for deletion");
                    TempData["Error"] = "Truyện không tồn tại hoặc đã bị xóa trước đó.";
                    return RedirectToAction("MangaManagement");
                }

                // Log manga details before deletion
                _logger.LogInformation($"Deleting manga: ID={mangaId}, Title={manga.Title}, Chapters={manga.ChapterCount}");

                // Use the repository to delete the manga and all related data
                await _mangaRepository.DeleteMangaAsync(mangaId);

                _logger.LogInformation($"Successfully deleted manga with ID {mangaId}");
                TempData["Message"] = $"Truyện '{manga.Title}' đã được xóa thành công cùng với tất cả dữ liệu liên quan.";

                return RedirectToAction("MangaManagement");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting manga with ID {mangaId}");
                TempData["Error"] = $"Xảy ra lỗi khi xóa truyện: {ex.Message}";
                return RedirectToAction("MangaManagement");
            }
        }

        [HttpPost("chapter/delete")]
        public async Task<IActionResult> DeleteChapter(int chapterId, int mangaId)
        {
            try
            {
                _logger.LogInformation($"Attempting to delete chapter with ID {chapterId}");

                // Get chapter details for logging
                var chapter = await _dbContext.Chapters
                    .FirstOrDefaultAsync(c => c.ChapterId == chapterId);

                if (chapter == null)
                {
                    _logger.LogWarning($"Chapter with ID {chapterId} not found for deletion");
                    TempData["Error"] = "Chương không tồn tại hoặc đã bị xóa trước đó.";
                    return RedirectToAction("MangaDetails", new { mangaId });
                }

                // Log chapter details before deletion
                _logger.LogInformation($"Deleting chapter: ID={chapterId}, Number={chapter.ChapterNumber}, Title={chapter.Title}");

                // Use the repository to delete the chapter and all related data
                await _chapterRepository.DeleteChapterAsync(chapterId);

                // Update the chapter count for the manga
                var manga = await _dbContext.Mangas.FindAsync(mangaId);
                if (manga != null)
                {
                    manga.ChapterCount = await _dbContext.Chapters.CountAsync(c => c.MangaId == mangaId);
                    await _dbContext.SaveChangesAsync();
                }

                _logger.LogInformation($"Successfully deleted chapter with ID {chapterId}");
                TempData["Message"] = $"Chương {chapter.ChapterNumber} đã được xóa thành công cùng với tất cả dữ liệu liên quan.";

                return RedirectToAction("MangaDetails", new { mangaId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting chapter with ID {chapterId}");
                TempData["Error"] = $"Xảy ra lỗi khi xóa chương: {ex.Message}";
                return RedirectToAction("MangaDetails", new { mangaId });
            }
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> Statistics()
        {
            try
            {
                // Cập nhật thống kê để đảm bảo dữ liệu mới nhất
                await _statsService.UpdateStatsAsync();

                // Thống kê tổng quan
                var basicStats = await _statsService.GetBasicStatsAsync();

                // Default period for initial data load
                var defaultPeriod = "month";

                // Get views data for the default period
                var viewsData = await _statsService.GetViewsDataByPeriodAsync(defaultPeriod);

                // Get user growth data for the default period
                var userGrowthData = await _statsService.GetUserGrowthByPeriodAsync(defaultPeriod);

                // Get top manga for last 30 days
                var today = DateTime.UtcNow;
                var thirtyDaysAgo = today.AddDays(-30);
                var topManga = await _statsService.GetTopMangaByTimeRangeAsync(thirtyDaysAgo, today, 10);

                // Get genre distribution
                var genreDistribution = await _statsService.GetGenreDistributionAsync();

                // Transform genre distribution for chart.js
                var genreLabels = new List<string>();
                var genreData = new List<int>();

                foreach (var entry in genreDistribution)
                {
                    genreLabels.Add(entry.Key);
                    genreData.Add(entry.Value);
                }

                // Pass data to view
                ViewBag.BasicStats = basicStats;
                ViewBag.TopManga = topManga.Select(m => new {
                    m.MangaId,
                    m.Title,
                    m.CoverUrl,
                    m.ViewCount
                }).ToList();
                ViewBag.ViewsData = viewsData;
                ViewBag.UserGrowthData = userGrowthData;
                ViewBag.GenreDistribution = new { labels = genreLabels, data = genreData };
                ViewBag.DefaultPeriod = defaultPeriod;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Statistics page");
                return RedirectToAction("Error");
            }
        }

        [HttpGet("sync-services")]
        public IActionResult SyncServices()
        {
            return View();
        }

        [HttpPost("sync-vietnamese-manga")]
        public async Task<IActionResult> SyncVietnameseManga([FromForm] int limit = 10)
        {
            _logger.LogInformation($"Đồng bộ thủ công {limit} truyện tiếng Việt được cập nhật gần đây nhất từ MangaDex được yêu cầu");

            await _syncService.SyncLatestVietnameseMangaAsync(limit, syncChapters: true);

            TempData["Message"] = $"Đã đồng bộ thành công {limit} truyện tiếng Việt mới nhất (cập nhật gần đây, kèm chapter và nội dung chapter) từ MangaDex! Thông tin UpdatedAt và ChapterCount đã được cập nhật.";

            return RedirectToAction("SyncServices");
        }

        [HttpPost("fix-future-dates")]
        public async Task<IActionResult> FixFutureDates()
        {
            _logger.LogInformation("Bắt đầu kiểm tra và sửa các ngày không hợp lệ");

            try
            {
                // Tìm các manga có ngày trong tương lai xa (sau 2030)
                var futureDate = new DateTime(2030, 1, 1);
                var mangasWithFutureDates = await _dbContext.Mangas
                    .Where(m => m.UpdatedAt > futureDate || m.CreatedAt > futureDate)
                    .ToListAsync();

                if (!mangasWithFutureDates.Any())
                {
                    TempData["Message"] = "Không tìm thấy truyện nào có ngày không hợp lệ.";
                    return RedirectToAction("SyncServices");
                }

                int fixedCount = 0;
                foreach (var manga in mangasWithFutureDates)
                {
                    _logger.LogInformation($"Fixing future dates for manga {manga.MangaId}: {manga.Title}. Current: UpdatedAt={manga.UpdatedAt}, CreatedAt={manga.CreatedAt}");

                    var chapters = await _dbContext.Chapters
                        .Where(c => c.MangaId == manga.MangaId)
                        .OrderBy(c => c.UploadDate)
                        .ToListAsync();

                    if (chapters.Any())
                    {
                        // Có chapter, lấy ngày từ chapter
                        manga.CreatedAt = chapters.First().UploadDate;
                        manga.UpdatedAt = chapters.OrderByDescending(c => c.UploadDate).First().UploadDate;
                    }
                    else
                    {
                        // Không có chapter, dùng ngày hiện tại
                        manga.CreatedAt = DateTime.Now;
                        manga.UpdatedAt = DateTime.Now;
                    }

                    fixedCount++;
                    _logger.LogInformation($"Fixed dates for manga {manga.MangaId}: UpdatedAt={manga.UpdatedAt}, CreatedAt={manga.CreatedAt}");
                }

                await _dbContext.SaveChangesAsync();
                TempData["Message"] = $"Đã sửa ngày cho {fixedCount} truyện thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi sửa ngày không hợp lệ");
                TempData["Error"] = "Đã xảy ra lỗi khi sửa ngày không hợp lệ. Vui lòng kiểm tra logs.";
            }

            return RedirectToAction("SyncServices");
        }

        [HttpGet("settings")]
        public IActionResult Settings()
        {
            return View();
        }

        [HttpPost("users/{userId}/toggle-status")]
        public async Task<IActionResult> ToggleUserStatus(int userId)
        {
            try
            {
                var user = await _dbContext.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound();
                }

                if (user.IsAdmin)
                {
                    TempData["Error"] = "Không thể thay đổi trạng thái của người dùng Admin.";
                    return RedirectToAction("UserDetails", new { userId });
                }

                // Đảo ngược trạng thái kích hoạt
                user.IsActive = !user.IsActive;

                await _dbContext.SaveChangesAsync();

                string statusMessage = user.IsActive ? "kích hoạt" : "vô hiệu hóa";
                TempData["Message"] = $"Đã {statusMessage} tài khoản của người dùng {user.Username}.";

                return RedirectToAction("UserDetails", new { userId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error toggling status for user with ID {userId}");
                TempData["Error"] = "Đã xảy ra lỗi khi thay đổi trạng thái người dùng.";
                return RedirectToAction("UserManagement");
            }
        }

        [HttpPost("create-random-users")]
        public async Task<IActionResult> CreateRandomUsers(int count = 1)
        {
            try
            {
                // Giới hạn số lượng người dùng có thể tạo cùng một lúc
                count = Math.Min(count, 10);

                // Tạo danh sách để lưu các người dùng mới
                var newUsers = new List<User>();
                var random = new Random();

                // Các ký tự cho username và password
                const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

                // Định nghĩa phạm vi ngày tháng: từ 01/01/2024 đến hiện tại năm 2025
                DateTime startDate = new DateTime(2024, 1, 1);
                DateTime endDate = new DateTime(2025, 12, 31);
                int rangeDays = (endDate - startDate).Days;

                for (int i = 0; i < count; i++)
                {
                    // Tạo username ngẫu nhiên 3 ký tự
                    string username = new string(Enumerable.Repeat(chars, 3)
                        .Select(s => s[random.Next(s.Length)]).ToArray());

                    // Kiểm tra username đã tồn tại chưa
                    while (await _dbContext.Users.AnyAsync(u => u.Username == username))
                    {
                        username = new string(Enumerable.Repeat(chars, 3)
                            .Select(s => s[random.Next(s.Length)]).ToArray());
                    }

                    // Tạo password ngẫu nhiên 3 ký tự
                    string password = new string(Enumerable.Repeat(chars, 3)
                        .Select(s => s[random.Next(s.Length)]).ToArray());

                    // Tạo email dựa trên username
                    string email = $"{username}@example.com";

                    // Băm mật khẩu
                    string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

                    // Tạo ngày đăng ký ngẫu nhiên trong khoảng 2024-2025
                    DateTime randomDate = startDate.AddDays(random.Next(rangeDays));

                    // Tạo người dùng mới
                    var user = new User
                    {
                        Username = username,
                        Email = email,
                        PasswordHash = passwordHash,
                        CreatedAt = randomDate,
                        IsActive = true,
                        IsAdmin = false,
                        // Tự động tạo AvatarUrl giống như UserService
                        AvatarUrl = $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(username)}&background=random&color=fff"
                    };

                    newUsers.Add(user);

                    // Log thông tin người dùng mới (để admin có thể xem)
                    _logger.LogInformation($"Tạo người dùng ngẫu nhiên: Username = {username}, Password = {password}, Email = {email}, Ngày tạo = {randomDate.ToString("dd/MM/yyyy")}");
                }

                // Thêm người dùng vào database
                await _dbContext.Users.AddRangeAsync(newUsers);
                await _dbContext.SaveChangesAsync();

                TempData["Message"] = $"Đã tạo thành công {count} người dùng ngẫu nhiên. Xem logs để biết thông tin đăng nhập.";

                return RedirectToAction("UserManagement");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo người dùng ngẫu nhiên");
                TempData["Error"] = "Đã xảy ra lỗi khi tạo người dùng ngẫu nhiên.";
                return RedirectToAction("UserManagement");
            }
        }

        [HttpGet("random-data-generator")]
        public IActionResult RandomDataGenerator()
        {
            return View();
        }

        [HttpPost("create-random-views")]
        public async Task<IActionResult> CreateRandomViews(int mangaId = 0, int count = 100)
        {
            try
            {
                // Giới hạn số lượng lượt xem có thể tạo cùng một lúc
                count = Math.Min(count, 1000);

                var random = new Random();
                var viewCounts = new List<ViewCount>();

                // Định nghĩa phạm vi ngày tháng: từ 21/04/2024 đến 21/04/2025
                DateTime endDate = new DateTime(2025, 4, 21);
                DateTime startDate = endDate.AddYears(-1);
                int rangeDays = (endDate - startDate).Days;

                // Lấy danh sách manga (tất cả hoặc chỉ 1 manga cụ thể)
                var mangaQuery = _dbContext.Mangas.AsQueryable();
                if (mangaId > 0)
                {
                    mangaQuery = mangaQuery.Where(m => m.MangaId == mangaId);
                }

                var mangas = await mangaQuery.ToListAsync();
                if (!mangas.Any())
                {
                    TempData["Error"] = "Không tìm thấy truyện nào!";
                    return RedirectToAction("RandomDataGenerator");
                }

                // Lấy danh sách users và chapters
                var users = await _dbContext.Users.Where(u => !u.IsAdmin).ToListAsync();
                if (!users.Any())
                {
                    TempData["Error"] = "Không tìm thấy người dùng nào!";
                    return RedirectToAction("RandomDataGenerator");
                }

                // Lấy danh sách IP tạm (ngẫu nhiên) để tạo lượt xem
                var ipAddresses = new List<string>();
                for (int i = 0; i < 50; i++)
                {
                    ipAddresses.Add($"192.168.{random.Next(1, 255)}.{random.Next(1, 255)}");
                }

                int createdCount = 0;
                Dictionary<int, int> mangaViewCounts = new Dictionary<int, int>();

                // Chuẩn bị từ điển để theo dõi số lượt xem đã tạo cho mỗi truyện
                foreach (var manga in mangas)
                {
                    mangaViewCounts[manga.MangaId] = 0;
                }

                // Nếu chỉ định một manga cụ thể, tạo toàn bộ số lượt xem cho truyện đó
                if (mangaId > 0)
                {
                    await CreateViewsForManga(mangas[0], count, users, ipAddresses, startDate, rangeDays);

                    // Cập nhật lại ViewCount trong bảng Manga
                    int viewCount = await _dbContext.ViewCounts.CountAsync(v => v.MangaId == mangaId);
                    mangas[0].ViewCount = viewCount;
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation($"Đã tạo thành công {count} lượt xem ngẫu nhiên cho truyện {mangas[0].Title}.");
                    TempData["Message"] = $"Đã tạo thành công {count} lượt xem ngẫu nhiên cho truyện {mangas[0].Title}.";
                }
                else
                {
                    // Tạo X lượt xem cho X truyện (mỗi truyện có số lượt xem ngẫu nhiên)
                    int totalCreatedViews = 0;
                    foreach (var manga in mangas)
                    {
                        // Tạo số lượt xem ngẫu nhiên cho truyện này (ví dụ: từ 1 đến count)
                        int randomViewCountForManga = random.Next(1, count + 1);

                        await CreateViewsForManga(manga, randomViewCountForManga, users, ipAddresses, startDate, rangeDays);

                        // Cập nhật lại ViewCount trong bảng Manga sau khi tạo lượt xem
                        int viewCount = await _dbContext.ViewCounts.CountAsync(v => v.MangaId == manga.MangaId);
                        manga.ViewCount = viewCount;
                        totalCreatedViews += randomViewCountForManga; // Cộng dồn số lượt xem thực tế đã cố gắng tạo
                    }
                    await _dbContext.SaveChangesAsync(); // Lưu thay đổi ViewCount cho tất cả manga

                    _logger.LogInformation($"Đã tạo thành công lượt xem ngẫu nhiên cho {mangas.Count} truyện (tổng cộng khoảng {totalCreatedViews} lượt xem).");
                    TempData["Message"] = $"Đã tạo thành công lượt xem ngẫu nhiên cho {mangas.Count} truyện (tổng cộng khoảng {totalCreatedViews} lượt xem).";
                }

                return RedirectToAction("RandomDataGenerator");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo lượt xem ngẫu nhiên");
                TempData["Error"] = "Đã xảy ra lỗi khi tạo lượt xem ngẫu nhiên. Vui lòng kiểm tra logs.";
                return RedirectToAction("RandomDataGenerator");
            }
        }

        // Phương thức phụ để tạo lượt xem cho một truyện cụ thể
        private async Task CreateViewsForManga(Manga manga, int count, List<User> users, List<string> ipAddresses, DateTime startDate, int rangeDays)
        {
            var random = new Random();
            int createdCount = 0;
            int attempts = 0;
            int maxAttempts = count * 3; // Giới hạn số lần thử để tránh vòng lặp vô hạn

            // Lưu trữ các khóa đã được tạo để tránh trùng lặp
            HashSet<string> existingKeys = new HashSet<string>();

            // Lấy danh sách chapter của manga
            var chapters = await _dbContext.Chapters
                .Where(c => c.MangaId == manga.MangaId)
                .ToListAsync();

            if (!chapters.Any())
            {
                _logger.LogWarning($"Truyện {manga.Title} (ID: {manga.MangaId}) không có chapter nào, không thể tạo lượt xem.");
                return;
            }

            // Sắp xếp chapters theo thứ tự Chapter Number để phân phối lượt xem hợp lý hơn
            var sortedChapters = chapters.OrderBy(c => c.ChapterNumber).ToList();

            // Tạo phân phối ngẫu nhiên cho các chapter (chapters đầu có xu hướng có nhiều lượt xem hơn)
            Dictionary<int, int> chapterWeights = new Dictionary<int, int>();
            for (int i = 0; i < sortedChapters.Count; i++)
            {
                // Chapters đầu tiên có trọng số cao hơn (nhiều lượt xem hơn) so với chapters sau
                chapterWeights[sortedChapters[i].ChapterId] = sortedChapters.Count - i + 5;
            }

            // Tổng trọng số để tính xác suất
            int totalWeight = chapterWeights.Values.Sum();

            // Tải các khóa ViewCount đã tồn tại trong cơ sở dữ liệu
            var existingViewKeys = await _dbContext.ViewCounts
                .Where(v => v.MangaId == manga.MangaId)
                .Select(v => $"{v.ChapterId}|{v.IpAddress}|{v.ViewedAt.Ticks}")
                .ToListAsync();

            foreach (var key in existingViewKeys)
            {
                existingKeys.Add(key);
            }

            while (createdCount < count && attempts < maxAttempts)
            {
                attempts++;

                // Tạo batch để xử lý theo từng nhóm nhỏ
                var viewCountBatch = new List<ViewCount>();
                int batchSize = Math.Min(50, count - createdCount);

                for (int i = 0; i < batchSize; i++)
                {
                    // Chọn chapter dựa trên trọng số
                    int randomValue = random.Next(totalWeight);
                    int chapterIdx = 0;
                    int weightSum = 0;

                    // Lựa chọn chapter dựa trên trọng số
                    foreach (var currentChapter in sortedChapters) // Renamed loop variable
                    {
                        weightSum += chapterWeights[currentChapter.ChapterId];
                        if (randomValue < weightSum)
                        {
                            chapterIdx = sortedChapters.IndexOf(currentChapter);
                            break;
                        }
                    }

                    var selectedChapter = sortedChapters[chapterIdx]; // Renamed variable

                    // Tạo phân phối hợp lý theo thời gian - chapter mới thường có ngày xem gần đây hơn
                    DateTime viewDate;

                    // Nếu đây là một chapter mới (nửa sau của danh sách), ưu tiên ngày xem gần đây hơn
                    if (chapterIdx >= sortedChapters.Count / 2)
                    {
                        // Chapters mới hơn có xu hướng được đọc gần đây hơn
                        int daysOffset = rangeDays / 2 + random.Next(rangeDays / 2);
                        viewDate = startDate.AddDays(daysOffset);
                    }
                    else
                    {
                        // Chapters cũ hơn có thể được đọc bất cứ lúc nào trong khoảng thời gian
                        viewDate = startDate.AddDays(random.Next(rangeDays));
                    }

                    // Thêm giờ, phút, giây ngẫu nhiên
                    viewDate = viewDate
                        .AddHours(random.Next(24))
                        .AddMinutes(random.Next(60))
                        .AddSeconds(random.Next(60));

                    // Quyết định xem lượt xem có liên kết với user hay không (70% có user)
                    bool hasUser = random.Next(100) < 70;
                    User user = null;

                    // Người dùng thường đọc nhiều chapter liên tiếp, nhưng ở đây chúng ta tạo ngẫu nhiên
                    if (hasUser && users.Any())
                    {
                        user = users[random.Next(users.Count)];
                    }

                    // Lấy IP ngẫu nhiên - cùng IP có xu hướng đọc nhiều chapter
                    string ipAddress = ipAddresses[random.Next(ipAddresses.Count)];

                    // Tạo khóa duy nhất để kiểm tra trùng lặp
                    string viewKey = $"{selectedChapter.ChapterId}|{ipAddress}|{viewDate.Ticks}"; // Use renamed variable

                    // Kiểm tra xem lượt xem này đã tồn tại trong HashSet chưa
                    if (!existingKeys.Contains(viewKey))
                    {
                        // Tạo lượt xem mới
                        var viewCount = new ViewCount
                        {
                            MangaId = manga.MangaId,
                            ChapterId = selectedChapter.ChapterId, // Use renamed variable
                            UserId = hasUser ? user.UserId : (int?)null,
                            IpAddress = ipAddress,
                            ViewedAt = viewDate
                        };

                        viewCountBatch.Add(viewCount);
                        existingKeys.Add(viewKey);
                        createdCount++;
                    }
                }

                // Lưu batch hiện tại vào cơ sở dữ liệu
                if (viewCountBatch.Any())
                {
                    try
                    {
                        await _dbContext.ViewCounts.AddRangeAsync(viewCountBatch);
                        await _dbContext.SaveChangesAsync();

                        // Đặt lại DbContext để tránh vấn đề theo dõi thực thể
                        _dbContext.ChangeTracker.Clear();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Lỗi khi thêm lượt xem cho truyện {manga.Title} (ID: {manga.MangaId})");
                        // Tiếp tục thử với batch tiếp theo
                    }
                }
            }

            if (createdCount < count)
            {
                _logger.LogWarning($"Chỉ có thể tạo {createdCount}/{count} lượt xem cho truyện {manga.Title} (ID: {manga.MangaId}) sau {attempts} lần thử");
            }
        }

        [HttpPost("create-random-favorites")]
        public async Task<IActionResult> CreateRandomFavorites(int mangaId = 0, int count = 50)
        {
            try
            {
                // Giới hạn số lượng yêu thích có thể tạo cùng một lúc
                count = Math.Min(count, 500);

                var random = new Random();

                // Định nghĩa phạm vi ngày tháng: từ 21/04/2024 đến 21/04/2025
                DateTime endDate = new DateTime(2025, 4, 21);
                DateTime startDate = endDate.AddYears(-1);
                int rangeDays = (endDate - startDate).Days;

                // Lấy danh sách manga (tất cả hoặc chỉ 1 manga cụ thể)
                var mangaQuery = _dbContext.Mangas.AsQueryable();
                if (mangaId > 0)
                {
                    mangaQuery = mangaQuery.Where(m => m.MangaId == mangaId);
                }

                var mangas = await mangaQuery.ToListAsync();
                if (!mangas.Any())
                {
                    TempData["Error"] = "Không tìm thấy truyện nào!";
                    return RedirectToAction("RandomDataGenerator");
                }

                // Lấy danh sách users
                var users = await _dbContext.Users.Where(u => !u.IsAdmin).ToListAsync();
                if (!users.Any())
                {
                    TempData["Error"] = "Không tìm thấy người dùng nào!";
                    return RedirectToAction("RandomDataGenerator");
                }

                int totalCreated = 0;

                // Nếu chỉ định một manga cụ thể, tạo toàn bộ số lượt yêu thích cho truyện đó
                if (mangaId > 0)
                {
                    int created = await CreateFavoritesForManga(mangas[0], count, users, startDate, rangeDays);

                    _logger.LogInformation($"Đã tạo thành công {created} lượt yêu thích ngẫu nhiên cho truyện {mangas[0].Title}.");
                    TempData["Message"] = $"Đã tạo thành công {created} lượt yêu thích ngẫu nhiên cho truyện {mangas[0].Title}.";
                }
                else
                {
                    // Tạo X lượt yêu thích cho X truyện (mỗi truyện có count lượt yêu thích)
                    foreach (var manga in mangas)
                    {
                        int created = await CreateFavoritesForManga(manga, count, users, startDate, rangeDays);
                        totalCreated += created;
                    }

                    _logger.LogInformation($"Đã tạo thành công khoảng {count} lượt yêu thích ngẫu nhiên cho {mangas.Count} truyện (tổng cộng {totalCreated} lượt yêu thích).");
                    TempData["Message"] = $"Đã tạo thành công khoảng {count} lượt yêu thích ngẫu nhiên cho {mangas.Count} truyện (tổng cộng {totalCreated} lượt yêu thích).";
                }

                return RedirectToAction("RandomDataGenerator");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo yêu thích ngẫu nhiên");
                TempData["Error"] = "Đã xảy ra lỗi khi tạo yêu thích ngẫu nhiên. Vui lòng kiểm tra logs.";
                return RedirectToAction("RandomDataGenerator");
            }
        }

        // Phương thức phụ để tạo yêu thích cho một truyện cụ thể
        private async Task<int> CreateFavoritesForManga(Manga manga, int count, List<User> users, DateTime startDate, int rangeDays)
        {
            var random = new Random();
            var favorites = new List<Favorite>();
            int createdCount = 0;

            // Tạo danh sách ngẫu nhiên các user để thêm yêu thích
            // Nếu số lượng yêu thích cần tạo lớn hơn số lượng user, thì sẽ chỉ tạo tối đa bằng số lượng user
            int maxPossibleFavorites = Math.Min(count, users.Count);

            // Trộn ngẫu nhiên danh sách users để lấy các user khác nhau
            var shuffledUsers = users.OrderBy(u => random.Next()).Take(maxPossibleFavorites).ToList();

            foreach (var user in shuffledUsers)
            {
                // Kiểm tra xem user đã yêu thích manga này chưa
                bool exists = await _dbContext.Favorites.AnyAsync(f =>
                    f.UserId == user.UserId && f.MangaId == manga.MangaId);

                if (!exists)
                {
                    // Tạo ngày yêu thích ngẫu nhiên trong khoảng một năm qua
                    DateTime favoriteDate = startDate.AddDays(random.Next(rangeDays));

                    // Tạo yêu thích mới
                    var favorite = new Favorite
                    {
                        UserId = user.UserId,
                        MangaId = manga.MangaId,
                        CreatedAt = favoriteDate
                    };

                    favorites.Add(favorite);
                    createdCount++;

                    // Thêm các yêu thích theo batch để tăng hiệu suất
                    if (favorites.Count >= 100)
                    {
                        await _dbContext.Favorites.AddRangeAsync(favorites);
                        await _dbContext.SaveChangesAsync();
                        favorites.Clear();
                    }
                }

                // Nếu đã đạt đủ số lượng yêu thích cần tạo thì dừng lại
                if (createdCount >= count)
                {
                    break;
                }
            }

            // Lưu các yêu thích còn lại
            if (favorites.Any())
            {
                await _dbContext.Favorites.AddRangeAsync(favorites);
                await _dbContext.SaveChangesAsync();
            }

            return createdCount;
        }
    }
}