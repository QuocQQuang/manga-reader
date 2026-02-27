using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Mangareading.DTOs;
using Mangareading.Models;
using Mangareading.Repositories.Interfaces;

namespace Mangareading.Services
{
    public class MangaDexService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _memoryCache;
        private readonly ApiCacheService _apiCacheService;
        private readonly ILogger<MangaDexService> _logger;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly JsonSerializerOptions _jsonOptions;

        // Semaphore cho đồng bộ truy cập API
        private readonly SemaphoreSlim _apiSemaphore = new SemaphoreSlim(3, 3);
        // Rate limiter
        private readonly Dictionary<string, DateTime> _lastRequestTimes = new Dictionary<string, DateTime>();
        private const int MIN_REQUEST_INTERVAL_MS = 300;

        public MangaDexService(
            HttpClient httpClient,
            IMemoryCache memoryCache,
            ApiCacheService apiCacheService,
            ILogger<MangaDexService> logger)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://api.mangadex.org");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "MangaReader/1.0");
            _memoryCache = memoryCache;
            _apiCacheService = apiCacheService;
            _logger = logger;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<List<MangaDto>> GetMangaDexMangaListAsync(string searchQuery = null, int limit = 0, string[] genres = null, string[] excludedGenres = null, string language = "vi")
        {
            // Tạo cache key từ các tham số
            string cacheKey = $"manga_list_{searchQuery}_{string.Join("_", genres ?? new string[0])}_{string.Join("_", excludedGenres ?? new string[0])}_{language}";

            // Sử dụng phương thức GetOrUpdateCache mới
            return await _apiCacheService.GetOrUpdateCacheAsync<List<MangaDto>>(
                cacheKey,
                async () => await FetchMangaListFromApiAsync(searchQuery, limit, genres, excludedGenres, language),
                TimeSpan.FromHours(3)
            );
        }

        private async Task<List<MangaDto>> FetchMangaListFromApiAsync(string searchQuery = null, int limit = 0, string[] genres = null, string[] excludedGenres = null, string language = "vi")
        {
            try
            {
                _logger.LogInformation("Fetching manga list from MangaDex API");

                // Chuẩn bị danh sách để lưu tất cả manga
                List<MangaDto> allMangas = new List<MangaDto>();
                int offset = 0;
                int apiLimit = 100; // MangaDex giới hạn 100 kết quả mỗi request
                bool hasMore = true;
                int totalFetched = 0;
                int maxResults = limit > 0 ? limit : 1000; // Giới hạn số lượng manga cần lấy, mặc định 1000

                // Xây dựng URL cơ bản
                string baseUrl = "/manga?includes[]=cover_art&includes[]=author&includes[]=artist&order[updatedAt]=desc";
                if (language != "all")
                {
                    baseUrl += $"&availableTranslatedLanguage[]={language}";
                }

                // Thêm điều kiện tìm kiếm
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    baseUrl += $"&title={Uri.EscapeDataString(searchQuery)}";
                }

                // Thêm thể loại nếu có
                if (genres != null && genres.Length > 0)
                {
                    foreach (var genre in genres)
                    {
                        baseUrl += $"&includedTags[]={genre}";
                    }
                }

                // Thêm thể loại loại trừ nếu có
                if (excludedGenres != null && excludedGenres.Length > 0)
                {
                    foreach (var genre in excludedGenres)
                    {
                        baseUrl += $"&excludedTags[]={genre}";
                    }
                }

                // Lặp qua từng trang cho đến khi lấy hết tất cả manga hoặc đạt giới hạn
                while (hasMore && totalFetched < maxResults)
                {
                    // Thêm phân trang vào URL
                    string url = $"{baseUrl}&limit={apiLimit}&offset={offset}";

                    // Thêm delay để tránh rate limit
                    if (offset > 0)
                    {
                        await ThrottleRequestAsync("manga_list");
                    }

                    _logger.LogInformation($"Fetching manga with offset {offset}, total so far: {totalFetched}");

                    var response = await GetFromApiAsync<MangaDexResponse<List<MangaDto>>>(url);

                    if (response?.Data == null || !response.Data.Any())
                    {
                        // Không còn kết quả nào nữa
                        hasMore = false;
                    }
                    else
                    {
                        // Thêm manga vào danh sách kết quả
                        allMangas.AddRange(response.Data);
                        totalFetched = allMangas.Count;

                        // Kiểm tra nếu đã nhận đủ dữ liệu hoặc số lượng ít hơn limit
                        if (response.Data.Count < apiLimit)
                        {
                            hasMore = false;
                        }
                        else
                        {
                            // Di chuyển đến trang tiếp theo
                            offset += apiLimit;
                        }

                        _logger.LogInformation($"Fetched {response.Data.Count} mangas, total: {totalFetched}, offset: {offset}");

                        // Kiểm tra nếu đã đạt đủ số lượng cần lấy
                        if (totalFetched >= maxResults)
                        {
                            hasMore = false;
                            // Cắt bớt nếu vượt quá giới hạn
                            if (totalFetched > maxResults)
                            {
                                allMangas = allMangas.Take(maxResults).ToList();
                            }
                        }
                    }
                }

                _logger.LogInformation($"Successfully fetched {allMangas.Count} total mangas");
                return allMangas;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching manga list from MangaDex");
                return new List<MangaDto>();
            }
        }

        public async Task<List<MangaDto>> GetLatestVietnameseMangaAsync(int limit = 10)
        {
            string cacheKey = $"latest_vietnamese_manga_{limit}";
            
            // Sử dụng phương thức GetOrUpdateCache mới
            return await _apiCacheService.GetOrUpdateCacheAsync<List<MangaDto>>(
                cacheKey,
                async () => await FetchLatestVietnameseMangaAsync(limit),
                TimeSpan.FromHours(1)
            );
        }

        private async Task<List<MangaDto>> FetchLatestVietnameseMangaAsync(int limit = 10)
        {
            try
            {
                await _semaphore.WaitAsync();
                _logger.LogInformation("Fetching latest Vietnamese manga from MangaDex API");

                // Trực tiếp lấy danh sách manga với các tham số lọc theo ngôn ngữ Tiếng Việt
                List<MangaDto> mangaList = new List<MangaDto>();
                int offset = 0;
                int apiLimit = 100; // MangaDex giới hạn 100 kết quả mỗi request
                bool hasMore = true;
                int maxToFetch = Math.Min(limit * 2, 100); // Lấy nhiều hơn một chút để có thể lọc

                while (hasMore && mangaList.Count < maxToFetch)
                {
                    // Sử dụng URL tương tự như bạn đã đề xuất
                    var url = $"/manga?limit={apiLimit}&offset={offset}&availableTranslatedLanguage[]=vi&includes[]=cover_art&includes[]=author&includes[]=artist&order[updatedAt]=desc&contentRating[]=safe&contentRating[]=suggestive";
                    
                    _logger.LogInformation($"Fetching manga with offset {offset}, total so far: {mangaList.Count}");
                    
                    // Thêm delay để tránh rate limit
                    if (offset > 0)
                    {
                        await ThrottleRequestAsync("latest_manga");
                    }
                    
                    var response = await GetFromApiAsync<MangaDexResponse<List<MangaDto>>>(url);

                    if (response?.Data == null || !response.Data.Any())
                    {
                        // Không còn kết quả nào nữa
                        hasMore = false;
                    }
                    else
                    {
                        // Thêm manga vào danh sách kết quả
                        mangaList.AddRange(response.Data);
                        
                        // Kiểm tra nếu đã nhận đủ dữ liệu
                        if (response.Data.Count < apiLimit)
                        {
                            hasMore = false;
                        }
                        else
                        {
                            // Di chuyển đến trang tiếp theo
                            offset += apiLimit;
                        }
                        
                        _logger.LogInformation($"Fetched {response.Data.Count} mangas, total: {mangaList.Count}");
                        
                        // Kiểm tra nếu đã đủ số lượng cần thiết
                        if (mangaList.Count >= maxToFetch)
                        {
                            hasMore = false;
                        }
                    }
                }

                if (mangaList.Count == 0)
                {
                    _logger.LogWarning("No Vietnamese manga found from MangaDex API");
                    return new List<MangaDto>();
                }

                // Giới hạn số lượng manga trả về
                var finalMangaList = mangaList.Take(limit).ToList();
                
                _logger.LogInformation($"Successfully fetched {finalMangaList.Count} Vietnamese manga from MangaDex API");
                return finalMangaList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching latest Vietnamese manga from MangaDex");
                return new List<MangaDto>();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<MangaDto> GetMangaByIdAsync(string mangaId)
        {
            if (string.IsNullOrEmpty(mangaId))
            {
                _logger.LogWarning("Empty manga ID provided");
                return null;
            }

            string cacheKey = $"manga_{mangaId}";
            
            // Sử dụng phương thức GetOrUpdateCache mới
            return await _apiCacheService.GetOrUpdateCacheAsync<MangaDto>(
                cacheKey,
                async () => 
                {
                    await ThrottleRequestAsync($"manga_{mangaId}"); 
                    var url = $"/manga/{mangaId}?includes[]=cover_art&includes[]=author&includes[]=artist";
                    var response = await GetFromApiAsync<MangaDexResponse<MangaDto>>(url);
                    return response?.Data;
                },
                TimeSpan.FromDays(1)
            );
        }

        public async Task<List<ChapterDto>> GetMangaChaptersAsync(string mangaId, string language = "vi")
        {
            string cacheKey = $"manga_chapters_{mangaId}_{language}";

            // Sử dụng phương thức GetOrUpdateCache mới
            return await _apiCacheService.GetOrUpdateCacheAsync<List<ChapterDto>>(
                cacheKey,
                async () => await FetchMangaChaptersAsync(mangaId, language),
                TimeSpan.FromHours(3)
            );
        }

        private async Task<List<ChapterDto>> FetchMangaChaptersAsync(string mangaId, string language = "vi")
        {
            try
            {
                _logger.LogInformation($"Fetching chapters for manga {mangaId} from MangaDex API");

                // Chuẩn bị danh sách để lưu tất cả chapters
                List<ChapterDto> allChapters = new List<ChapterDto>();
                int offset = 0;
                int limit = 100; // MangaDex giới hạn 100 kết quả mỗi request
                bool hasMore = true;

                // Lặp qua từng trang cho đến khi lấy hết tất cả chapters
                while (hasMore)
                {
                    // Xây dựng URL để lấy feed chapter của manga với phân trang
                    var url = $"/manga/{mangaId}/feed?limit={limit}&offset={offset}&translatedLanguage[]={language}&order[chapter]=asc";
                    if (language == "all")
                    {
                        url = $"/manga/{mangaId}/feed?limit={limit}&offset={offset}&order[chapter]=asc";
                    }

                    // Thêm delay để tránh rate limit
                    if (offset > 0)
                    {
                        await ThrottleRequestAsync($"chapters_{mangaId}");
                    }

                    var response = await GetFromApiAsync<MangaDexResponse<List<ChapterDto>>>(url);

                    if (response?.Data == null || !response.Data.Any())
                    {
                        // Không còn kết quả nào nữa
                        hasMore = false;
                    }
                    else
                    {
                        // Thêm chapters vào danh sách kết quả
                        allChapters.AddRange(response.Data);

                        // Kiểm tra nếu đã nhận đủ dữ liệu
                        if (response.Data.Count < limit)
                        {
                            hasMore = false;
                        }
                        else
                        {
                            // Di chuyển đến trang tiếp theo
                            offset += limit;
                        }

                        _logger.LogInformation($"Fetched {response.Data.Count} chapters, total: {allChapters.Count}, offset: {offset}");
                    }
                }

                if (!allChapters.Any())
                {
                    _logger.LogWarning($"No chapters found for manga {mangaId} with language {language}");
                    return new List<ChapterDto>();
                }

                // Lọc và sắp xếp các chapter theo số thứ tự
                var chapters = allChapters
                    .OrderBy(c => {
                        if (decimal.TryParse(c.Attributes.Chapter, out decimal chapterNumber))
                            return chapterNumber;
                        return 0;
                    })
                    .ToList();

                _logger.LogInformation($"Successfully fetched {chapters.Count} total chapters for manga {mangaId}");
                return chapters;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching chapters for manga {mangaId}");
                return new List<ChapterDto>();
            }
        }

        // Thêm phương thức để lấy nội dung của một chapter
        public async Task<ChapterReadDto> GetChapterContentAsync(string chapterId)
        {
            string cacheKey = $"chapter_content_{chapterId}";

            // Sử dụng phương thức GetOrUpdateCache mới
            return await _apiCacheService.GetOrUpdateCacheAsync<ChapterReadDto>(
                cacheKey,
                async () => 
                {
                    _logger.LogInformation($"Fetching content for chapter {chapterId} from MangaDex API");
                    
                    // Thêm delay cố định trước khi gọi API at-home để giảm khả năng bị rate limit
                    await Task.Delay(2000);
                    
                    // Thiết lập throttle dành riêng cho API at-home với thời gian chờ lâu hơn
                    await ThrottleRequestAsync($"chapter_content_{chapterId}", 800);
                    
                    // Gọi API để lấy server at-home
                    var url = $"/at-home/server/{chapterId}";
                    var result = await GetFromApiAsync<ChapterReadDto>(url);
                    
                    // Thêm delay sau khi lấy dữ liệu thành công từ at-home server
                    await Task.Delay(1500);
                    
                    return result;
                },
                TimeSpan.FromHours(12)
            );
        }

        public async Task<List<MangaDto>> SearchMangaByTitleAsync(string title, int limit = 5)
        {
            if (string.IsNullOrEmpty(title))
            {
                _logger.LogWarning("Empty title provided for manga search");
                return new List<MangaDto>();
            }

            string cacheKey = $"manga_search_{title.ToLower().Trim()}_{limit}";

            // Sử dụng phương thức GetOrUpdateCache mới
            return await _apiCacheService.GetOrUpdateCacheAsync<List<MangaDto>>(
                cacheKey,
                async () => 
                {
                    _logger.LogInformation($"Searching for manga with title: {title}");
                    await ThrottleRequestAsync("manga_search");
                    
                    // Tạo URL tìm kiếm với các tham số cần thiết
                    var searchUrl = $"/manga?title={Uri.EscapeDataString(title)}&limit={limit}&includes[]=cover_art&includes[]=author&includes[]=artist";
                    
                    // Thêm tùy chọn ưu tiên manga có tiếng Việt
                    searchUrl += "&availableTranslatedLanguage[]=vi";
                    
                    var response = await GetFromApiAsync<MangaDexResponse<List<MangaDto>>>(searchUrl);
                    
                    if (response?.Data == null || !response.Data.Any())
                    {
                        _logger.LogInformation($"No manga found for title: {title}");
                        return new List<MangaDto>();
                    }
                    
                    _logger.LogInformation($"Found {response.Data.Count} manga for title: {title}");
                    return response.Data;
                },
                TimeSpan.FromDays(1)
            );
        }

        private async Task<T> GetFromApiAsync<T>(string url)
        {
            await _apiSemaphore.WaitAsync();

            try
            {
                int maxRetries = 3;
                int retryDelay = 1000;

                for (int i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        var response = await _httpClient.GetAsync(url);

                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            return JsonSerializer.Deserialize<T>(content, _jsonOptions);
                        }

                        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        {
                            _logger.LogWarning($"Rate limit hit for {url}, waiting before retry");
                            // Tăng thời gian chờ nếu bị rate limit
                            await Task.Delay(retryDelay * (int)Math.Pow(2, i));
                            continue;
                        }

                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning($"API returned {response.StatusCode} for {url}: {errorContent}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (i == maxRetries - 1)
                        {
                            _logger.LogError(ex, $"All API request attempts failed for {url}");
                            throw;
                        }

                        _logger.LogWarning(ex, $"API request failed for {url}, retrying ({i + 1}/{maxRetries})");
                        await Task.Delay(retryDelay * (int)Math.Pow(2, i));
                    }
                }

                return default;
            }
            finally
            {
                _apiSemaphore.Release();
            }
        }

        // Phương thức giới hạn tốc độ gọi API để tránh rate limit
        private async Task ThrottleRequestAsync(string requestId, int minIntervalMs = MIN_REQUEST_INTERVAL_MS)
        {
            lock (_lastRequestTimes)
            {
                if (_lastRequestTimes.TryGetValue(requestId, out var lastRequest))
                {
                    var elapsed = (DateTime.UtcNow - lastRequest).TotalMilliseconds;
                    if (elapsed < minIntervalMs)
                    {
                        var waitTime = minIntervalMs - (int)elapsed;
                        if (waitTime > 0)
                        {
                            Task.Delay(waitTime).Wait();
                        }
                    }
                }
                _lastRequestTimes[requestId] = DateTime.UtcNow;
            }
            await Task.CompletedTask;
        }
    }
}