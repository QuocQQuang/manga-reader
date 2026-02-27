using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mangareading.Models;
using Mangareading.Repositories.Interfaces;

namespace Mangareading.Repositories
{
    public class ApiCacheRepository : IApiCacheRepository
    {
        private readonly IServiceProvider _serviceProvider;

        public ApiCacheRepository(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<ApiCache> GetCacheAsync(string key)
        {
            // Tạo scope mới và DbContext mới cho mỗi hoạt động truy vấn
            using (var scope = _serviceProvider.CreateScope())
            using (var context = scope.ServiceProvider.GetRequiredService<YourDbContext>())
            {
                return await context.ApiCache.FirstOrDefaultAsync(c => c.CacheKey == key);
            }
        }

        public async Task SaveCacheAsync(string key, string data, DateTime expireAt)
        {
            // Tạo scope mới và DbContext mới cho mỗi hoạt động ghi
            using (var scope = _serviceProvider.CreateScope())
            using (var context = scope.ServiceProvider.GetRequiredService<YourDbContext>())
            {
                var existingCache = await context.ApiCache.FirstOrDefaultAsync(c => c.CacheKey == key);

                if (existingCache == null)
                {
                    context.ApiCache.Add(new ApiCache
                    {
                        CacheKey = key,
                        CacheData = data,
                        ExpireAt = expireAt,
                        CreatedAt = DateTime.Now
                    });
                }
                else
                {
                    existingCache.CacheData = data;
                    existingCache.ExpireAt = expireAt;
                }

                await context.SaveChangesAsync();
            }
        }
    }
}