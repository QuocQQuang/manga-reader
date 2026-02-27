using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mangareading.Services
{
    public class MangaSyncBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MangaSyncBackgroundService> _logger;
        private readonly TimeSpan _syncInterval = TimeSpan.FromHours(6); // Đồng bộ mỗi 6 giờ

        public MangaSyncBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<MangaSyncBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MangaDex Background Sync Service đang khởi động...");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"Bắt đầu đồng bộ theo lịch trình tại: {DateTimeOffset.Now}");

                try
                {
                    // Tạo scope để đảm bảo lifetime services được quản lý đúng
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var syncService = scope.ServiceProvider.GetRequiredService<MangaSyncService>();
                        await syncService.SyncLatestVietnameseMangaAsync(10);
                    }

                    _logger.LogInformation("Đã hoàn thành đồng bộ theo lịch trình");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi thực hiện đồng bộ tự động");
                }

                // Đợi đến lần đồng bộ tiếp theo
                _logger.LogInformation($"Lần đồng bộ tiếp theo sẽ diễn ra trong {_syncInterval.TotalHours} giờ");
                await Task.Delay(_syncInterval, stoppingToken);
            }
        }
    }
}