using System;
using System.Threading.Tasks;
using Mangareading.Models;

namespace Mangareading.Repositories.Interfaces
{
    public interface IApiCacheRepository
    {
        Task<ApiCache> GetCacheAsync(string key);
        Task SaveCacheAsync(string key, string data, DateTime expireAt);
    }
}