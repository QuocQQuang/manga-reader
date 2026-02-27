using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mangareading.Utilities
{
    /// <summary>
    /// Class tiện ích giúp kiểm soát tốc độ request đến MangaDex API
    /// </summary>
    public static class MangaDexThrottler
    {
        private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(5, 5); // Giới hạn 5 request/giây
        private static DateTime _lastRequestTime = DateTime.MinValue;

        /// <summary>
        /// Đợi để đảm bảo không vượt quá rate limit của MangaDex API
        /// </summary>
        public static async Task WaitAsync()
        {
            await Semaphore.WaitAsync();

            try
            {
                // Đảm bảo mỗi request cách nhau ít nhất 200ms
                var timeSinceLastRequest = DateTime.Now - _lastRequestTime;
                if (timeSinceLastRequest.TotalMilliseconds < 200)
                {
                    await Task.Delay(200 - (int)timeSinceLastRequest.TotalMilliseconds);
                }

                _lastRequestTime = DateTime.Now;
            }
            finally
            {
                Semaphore.Release();
            }
        }
    }
}