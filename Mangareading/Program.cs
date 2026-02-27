using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.HttpOverrides;
using System;
using System.IO;
using Microsoft.EntityFrameworkCore; // Thêm namespace này
using Microsoft.EntityFrameworkCore.Infrastructure; // Thêm namespace này
using Mangareading.Models;
using Mangareading.Services; // Add this for FileLoggerProvider
using AWS.Logger.AspNetCore; // Add this for AddAWSProvider

namespace YourNamespace
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Tạo thư mục App_Data nếu chưa tồn tại
            var appDataDir = Path.Combine(Directory.GetCurrentDirectory(), "App_Data");
            if (!Directory.Exists(appDataDir))
            {
                Directory.CreateDirectory(appDataDir);
            }
            AppDomain.CurrentDomain.SetData("DataDirectory", appDataDir);

            var host = CreateHostBuilder(args).Build();

            // Auto-apply EF migrations and seed initial data on every startup
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();

                try
                {
                    logger.LogInformation("Applying database migrations...");
                    var db = services.GetRequiredService<YourDbContext>();
                    db.Database.Migrate();
                    logger.LogInformation("Migrations applied. Seeding data...");
                    Mangareading.Services.DbSeeder.SeedAsync(db, logger).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred during database migration/seeding. Check connection string in appsettings.json.");
                }
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var env = hostingContext.HostingEnvironment;

                    // Cấu hình thứ tự ưu tiên
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                          .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

                    // AWS EB biến môi trường có ưu tiên cao nhất
                    config.AddEnvironmentVariables();

                    if (args != null)
                    {
                        config.AddCommandLine(args);
                    }
                })
                .ConfigureLogging((hostContext, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();

                    // Cấu hình log vào file
                    var loggingConfig = hostContext.Configuration.GetSection("Logging:File");
                    if (loggingConfig.GetValue<bool>("Enabled"))
                    {
                        // Tạo thư mục logs nếu chưa tồn tại
                        var logPath = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                        if (!Directory.Exists(logPath))
                        {
                            Directory.CreateDirectory(logPath);
                        }

                        // Tạo file log theo ngày hiện tại
                        var today = DateTime.Now.ToString("yyyyMMdd");
                        var logFile = Path.Combine(logPath, $"app_{today}.log");

                        // Thêm provider ghi log vào file
                        // Only add EventLog on Windows platforms
                        if (OperatingSystem.IsWindows())
                        {
                            logging.AddEventLog();
                        }
                        logging.AddTraceSource("LogSource");

                        // Đăng ký event listener để ghi log vào file
                        var fileProvider = new FileLoggerProvider(logFile);
                        logging.Services.AddSingleton<ILoggerProvider>(fileProvider);
                    }

                    // Cấu hình log levels
                    if (hostContext.HostingEnvironment.IsProduction())
                    {
                        // Log vào CloudWatch khi chạy trên AWS
                        logging.AddAWSProvider(hostContext.Configuration.GetAWSLoggingConfigSection());

                        // Filter out SQL queries and EF Core logs
                        logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
                        logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None);
                        logging.AddFilter("System.Data.SqlClient", LogLevel.Warning);
                        logging.AddFilter("Microsoft.Data", LogLevel.Warning);

                        logging.AddFilter("Microsoft", LogLevel.Warning);
                        logging.AddFilter("System", LogLevel.Warning);
                    }
                    else
                    {
                        logging.AddDebug();

                        // Filter out SQL queries and EF Core logs in development too
                        logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
                        logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None);
                        logging.AddFilter("System.Data.SqlClient", LogLevel.Warning);
                        logging.AddFilter("Microsoft.Data", LogLevel.Warning);
                    }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();

                    // For AWS EB, check if running in production environment
                    var isProduction = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";

                    if (isProduction)
                    {
                        // AWS EB forwards requests from port 80/443 to the app
                        webBuilder.UseUrls("http://0.0.0.0:5000");
                    }
                    else
                    {
                        // In development, only use HTTPS on port 5001
                        webBuilder.UseUrls("https://0.0.0.0:5001");
                    }

                    webBuilder.ConfigureKestrel(options =>
                    {
                        // Tăng giới hạn kích thước request cho file uploads
                        options.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100 MB

                        // Tăng timeouts cho kết nối chậm
                        options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
                        options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
                    });
                });
    }
}