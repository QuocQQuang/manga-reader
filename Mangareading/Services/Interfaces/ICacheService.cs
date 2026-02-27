using System;
using System.Threading.Tasks;

namespace Mangareading.Services.Interfaces
{
    public interface ICacheService
    {
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> createFunc, TimeSpan? expiration = null);
        Task<bool> RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
    }
}