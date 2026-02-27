using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Mangareading.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace Mangareading.Services
{
    public class ApiCacheService
    {
        private readonly YourDbContext _context;
        private readonly ILogger<ApiCacheService> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly SemaphoreSlim _dbSemaphore = new SemaphoreSlim(1, 1);
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ConcurrentDictionary<string, DateTime> _cacheExpiryIndex = new ConcurrentDictionary<string, DateTime>();

        public ApiCacheService(
            YourDbContext context,
            ILogger<ApiCacheService> logger,
            IMemoryCache memoryCache)
        {
            _context = context;
            _logger = logger;
            _memoryCache = memoryCache;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// Kiểm tra xem cache key có tồn tại và còn hạn sử dụng không
        /// </summary>
        public async Task<bool> CacheExistsAsync(string cacheKey)
        {
            // Kiểm tra memory cache local
            if (_memoryCache.TryGetValue(cacheKey, out _))
            {
                return true;
            }

            // Kiểm tra trong index
            if (_cacheExpiryIndex.TryGetValue(cacheKey, out var expiryTime))
            {
                if (expiryTime > DateTime.Now)
                {
                    return true;
                }
                // Nếu hết hạn, xóa khỏi index
                _cacheExpiryIndex.TryRemove(cacheKey, out _);
            }

            try
            {
                await _dbSemaphore.WaitAsync();
                
                var exists = await _context.ApiCache
                    .AnyAsync(c => c.CacheKey == cacheKey && c.ExpireAt > DateTime.Now);
                
                if (exists)
                {
                    // Thêm vào index để tra cứu nhanh hơn sau này
                    var cacheItem = await _context.ApiCache
                        .FirstOrDefaultAsync(c => c.CacheKey == cacheKey);
                    
                    if (cacheItem != null)
                    {
                        _cacheExpiryIndex.TryAdd(cacheKey, cacheItem.ExpireAt);
                    }
                }
                
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi kiểm tra cache: {cacheKey}");
                return false;
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        /// <summary>
        /// Lấy dữ liệu từ cache trong database
        /// </summary>
        public async Task<T> GetFromCacheAsync<T>(string cacheKey) where T : class
        {
            // Kiểm tra memory cache trước
            if (_memoryCache.TryGetValue(cacheKey, out T memCachedData))
            {
                return memCachedData;
            }

            try
            {
                await _dbSemaphore.WaitAsync();
                
                var dbCache = await _context.ApiCache
                    .FirstOrDefaultAsync(c => c.CacheKey == cacheKey);

                if (dbCache != null && dbCache.ExpireAt > DateTime.Now)
                {
                    try
                    {
                        var data = JsonSerializer.Deserialize<T>(dbCache.CacheData, _jsonOptions);
                        if (data != null)
                        {
                            _logger.LogInformation($"Đã lấy dữ liệu từ cache: {cacheKey}");
                            
                            // Lưu vào memory cache
                            var cacheDuration = dbCache.ExpireAt - DateTime.Now;
                            _memoryCache.Set(cacheKey, data, cacheDuration);
                            
                            // Đảm bảo nó có trong index
                            _cacheExpiryIndex.TryAdd(cacheKey, dbCache.ExpireAt);
                            
                            return data;
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, $"Lỗi khi deserialize dữ liệu từ cache: {cacheKey}");
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi truy cập database cache: {cacheKey}");
                return null;
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        /// <summary>
        /// Lưu dữ liệu vào cache trong database
        /// </summary>
        public async Task SaveToCacheAsync<T>(string cacheKey, T data, TimeSpan expiration) where T : class
        {
            if (data == null)
            {
                _logger.LogWarning($"Không thể cache dữ liệu null: {cacheKey}");
                return;
            }

            try
            {
                // Lưu vào memory cache trước
                _memoryCache.Set(cacheKey, data, expiration);
                
                await _dbSemaphore.WaitAsync();

                var existingCache = await _context.ApiCache
                    .FirstOrDefaultAsync(c => c.CacheKey == cacheKey);

                string serializedData = JsonSerializer.Serialize(data);
                var expireAt = DateTime.Now.Add(expiration);

                if (existingCache == null)
                {
                    _context.ApiCache.Add(new ApiCache
                    {
                        CacheKey = cacheKey,
                        CacheData = serializedData,
                        ExpireAt = expireAt,
                        CreatedAt = DateTime.Now
                    });
                }
                else
                {
                    existingCache.CacheData = serializedData;
                    existingCache.ExpireAt = expireAt;
                }

                await _context.SaveChangesAsync();
                
                // Lưu thông tin expiry vào index
                _cacheExpiryIndex.AddOrUpdate(cacheKey, expireAt, (key, oldValue) => expireAt);
                
                _logger.LogInformation($"Đã cache dữ liệu: {cacheKey}, hết hạn sau {expiration.TotalHours:F1} giờ");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lưu dữ liệu vào cache: {cacheKey}");
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        /// <summary>
        /// Xóa một cache cụ thể từ database
        /// </summary>
        public async Task RemoveFromCacheAsync(string cacheKey)
        {
            try
            {
                // Xóa khỏi memory cache
                _memoryCache.Remove(cacheKey);
                
                // Xóa khỏi index
                _cacheExpiryIndex.TryRemove(cacheKey, out _);
                
                await _dbSemaphore.WaitAsync();

                var cache = await _context.ApiCache
                    .FirstOrDefaultAsync(c => c.CacheKey == cacheKey);

                if (cache != null)
                {
                    _context.ApiCache.Remove(cache);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Đã xóa cache: {cacheKey}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi xóa cache: {cacheKey}");
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }

        /// <summary>
        /// Xóa tất cả các cache đã hết hạn
        /// </summary>
        public async Task CleanupExpiredCacheAsync()
        {
            _logger.LogInformation("Bắt đầu dọn dẹp API cache đã hết hạn...");

            try
            {
                // Lấy thời gian hiện tại
                var now = DateTime.Now;
                
                // Xóa các key hết hạn từ index
                var expiredKeys = _cacheExpiryIndex.Where(kvp => kvp.Value < now).Select(kvp => kvp.Key).ToList();
                foreach (var key in expiredKeys)
                {
                    _cacheExpiryIndex.TryRemove(key, out _);
                    _memoryCache.Remove(key); // Đồng thời xóa từ memory cache
                }
                
                await _dbSemaphore.WaitAsync();

                // Tìm và xóa tất cả các cache đã hết hạn
                var expiredCaches = await _context.ApiCache
                    .Where(c => c.ExpireAt < now)
                    .ToListAsync();

                if (expiredCaches.Any())
                {
                    _logger.LogInformation($"Đã tìm thấy {expiredCaches.Count} cache đã hết hạn");

                    _context.ApiCache.RemoveRange(expiredCaches);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Đã xóa {expiredCaches.Count} cache đã hết hạn");
                }
                else
                {
                    _logger.LogInformation("Không có cache nào hết hạn cần xóa");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi dọn dẹp API cache đã hết hạn");
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }
        
        /// <summary>
        /// Kiểm tra và cập nhật cache nếu cần
        /// </summary>
        public async Task<T> GetOrUpdateCacheAsync<T>(string cacheKey, Func<Task<T>> fetchDataFunc, TimeSpan expiration) where T : class
        {
            // Kiểm tra cache
            var cachedData = await GetFromCacheAsync<T>(cacheKey);
            if (cachedData != null)
            {
                return cachedData;
            }
            
            // Nếu không có trong cache, lấy dữ liệu mới
            var data = await fetchDataFunc();
            
            // Lưu vào cache nếu dữ liệu hợp lệ
            if (data != null)
            {
                await SaveToCacheAsync(cacheKey, data, expiration);
            }
            
            return data;
        }
    }
}