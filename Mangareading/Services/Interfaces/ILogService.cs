using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mangareading.Models;

namespace Mangareading.Services.Interfaces
{
    public interface ILogService
    {
        Task<List<SystemLog>> GetRecentLogsAsync(int count = 50);
        Task<List<SystemLog>> GetLogsByLevelAsync(string level, int count = 50);
        Task<List<SystemLog>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate, int count = 100);
    }
}