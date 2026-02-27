using System;
using System.Threading;
using System.Threading.Tasks;
using Mangareading.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mangareading.Services
{
    public class StatsUpdateBackgroundService : BackgroundService
    {
        private readonly ILogger<StatsUpdateBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public StatsUpdateBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<StatsUpdateBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Stats Update Background Service is starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Cập nhật thống kê vào nửa đêm mỗi ngày
                    var now = DateTime.Now;
                    var nextRunTime = now.Date.AddDays(1); // Đặt thời gian chạy vào nửa đêm ngày mai
                    var delay = nextRunTime - now;

                    _logger.LogInformation($"Stats Update scheduled in {delay.TotalHours:F1} hours");

                    await Task.Delay(delay, stoppingToken);

                    // Cập nhật thống kê hàng ngày
                    await UpdateDailyStats();
                }
                catch (OperationCanceledException)
                {
                    // Dịch vụ đang dừng, không ghi log lỗi
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while updating stats");
                    
                    // Đợi 1 giờ trước khi thử lại nếu có lỗi
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
            
            _logger.LogInformation("Stats Update Background Service is stopping");
        }

        private async Task UpdateDailyStats()
        {
            _logger.LogInformation("Updating daily statistics...");
            
            using var scope = _serviceProvider.CreateScope();
            var statsService = scope.ServiceProvider.GetRequiredService<IStatsService>();
            
            await statsService.UpdateStatsAsync();
            
            _logger.LogInformation("Daily statistics updated successfully");
        }
    }
}