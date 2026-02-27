using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mangareading.Services
{
    public class ApiCacheCleanupService : BackgroundService
    {
        private readonly ILogger<ApiCacheCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ApiCacheCleanupService(
            IServiceProvider serviceProvider,
            ILogger<ApiCacheCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("API Cache Cleanup Service đang khởi động...");

            try
            {
                // Dọn dẹp cache khi ứng dụng khởi động
                await CleanupExpiredCacheAsync();
                
                // Tiếp tục dọn dẹp định kỳ mỗi 24 giờ
                while (!stoppingToken.IsCancellationRequested)
                {
                    // Chờ 24 giờ trước khi dọn dẹp lần tiếp theo
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                    
                    // Dọn dẹp cache đã hết hạn
                    await CleanupExpiredCacheAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // Dịch vụ đang dừng, không ghi log lỗi
                _logger.LogInformation("API Cache Cleanup Service đã dừng");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong API Cache Cleanup Service");
            }
        }

        private async Task CleanupExpiredCacheAsync()
        {
            _logger.LogInformation("Bắt đầu dọn dẹp API cache đã hết hạn...");
            
            using var scope = _serviceProvider.CreateScope();
            var apiCacheService = scope.ServiceProvider.GetRequiredService<ApiCacheService>();
            
            try
            {
                await apiCacheService.CleanupExpiredCacheAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi dọn dẹp API cache đã hết hạn");
            }
        }
    }
} 