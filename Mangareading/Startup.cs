using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Mangareading.Models;
using System;
using Mangareading.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Mangareading.Services.Interfaces;
using Mangareading.Repositories;
using Mangareading.Repositories.Interfaces;
using Mangareading.Middleware;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Logging;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        string connectionString = Configuration.GetConnectionString("DefaultConnection");

        // Kiểm tra biến môi trường dành riêng cho AWS
        string ebDbConnection = Environment.GetEnvironmentVariable("RDS_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(ebDbConnection))
        {
            connectionString = ebDbConnection;
        }

        // Configure JSON serialization
        services.AddControllersWithViews()
            .AddJsonOptions(options =>
            {
                // Use IgnoreCycles instead of Preserve to avoid reference objects in JSON
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.MaxDepth = 64; // Increase max depth if needed
            });

        // Add DbContext (replace with your actual DbContext)
        services.AddDbContext<YourDbContext>(options =>
       options.UseSqlServer(connectionString,
           sqlOptions => sqlOptions.EnableRetryOnFailure(
               maxRetryCount: 5,
               maxRetryDelay: TimeSpan.FromSeconds(30),
               errorNumbersToAdd: null
           )
       ), ServiceLifetime.Scoped
   );


        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";

                // Xử lý sự kiện để hiển thị trang lỗi tùy chỉnh thay vì chuyển hướng
                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = context =>
                    {
                        // Nếu là yêu cầu API, trả về 401 thay vì chuyển hướng
                        if (context.Request.Path.StartsWithSegments("/api") ||
                            context.Request.Headers["Accept"].ToString().Contains("application/json"))
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return Task.CompletedTask;
                        }

                        // Chuyển hướng đến trang Unauthorized thay vì trang đăng nhập
                        context.Response.Redirect("/Home/StatusCode?statusCode=401&returnUrl=" +
                            Uri.EscapeDataString(context.RedirectUri));
                        return Task.CompletedTask;
                    },

                    OnRedirectToAccessDenied = context =>
                    {
                        // Nếu là yêu cầu API, trả về 403 thay vì chuyển hướng
                        if (context.Request.Path.StartsWithSegments("/api") ||
                            context.Request.Headers["Accept"].ToString().Contains("application/json"))
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            return Task.CompletedTask;
                        }

                        // Chuyển hướng đến trang Forbidden thay vì trang AccessDenied
                        context.Response.Redirect("/Home/StatusCode?statusCode=403&returnUrl=" +
                            Uri.EscapeDataString(context.RedirectUri));
                        return Task.CompletedTask;
                    }
                };
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.HttpOnly = true;
            });

        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromHours(2);
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.IsEssential = true;
        });
        services.AddHttpClient("MangaDex", client => {
            client.BaseAddress = new Uri("https://api.mangadex.org");
            client.DefaultRequestHeaders.Add("User-Agent", "MangaReader/1.0");
            // Cấu hình timeout dài hơn nếu cần
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Register your IUserService
        services.AddScoped<IUserService, UserService>();
        services.AddMemoryCache();

        // HttpClient
        services.AddHttpClient<MangaDexService>();

        // Repositories
        services.AddScoped<IMangaRepository, MangaRepository>();
        services.AddScoped<IChapterRepository, ChapterRepository>();
        services.AddScoped<IViewCountRepository, ViewCountRepository>();
        services.AddScoped<IFavoriteRepository, FavoriteRepository>();
        services.AddScoped<IReadingHistoryRepository, ReadingHistoryRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();

        // Services
        services.AddScoped<MangaDexService>();
        services.AddScoped<MangaSyncService>();
        services.AddScoped<ApiCacheService>();
        services.AddScoped<IMangaStatisticsService, MangaStatisticsService>();
        services.AddScoped<IStatsService, StatsService>();
        services.AddScoped<ILogService, LogService>();
        services.AddScoped<IImgurService, ImgurService>();

        // Background Services
        // services.AddHostedService<MangaSyncBackgroundService>();
        services.AddHostedService<StatsUpdateBackgroundService>();
        services.AddHostedService<ApiCacheCleanupService>();

        // Add logging
        services.AddLogging();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
    {
        // Configure forwarded headers for reverse proxy scenarios
        var forwardedHeadersOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        };
        // Trust all proxies - adjust this for production if needed
        forwardedHeadersOptions.KnownNetworks.Clear();
        forwardedHeadersOptions.KnownProxies.Clear();
        app.UseForwardedHeaders(forwardedHeadersOptions);

        // Add security headers middleware
        app.UseSecurityHeaders();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        // Add Status Code Pages middleware to handle status codes like 404
        app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?statusCode={0}");
        //app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        // Sử dụng middleware phiên
        app.UseSession();

        // Add theme middleware after session middleware
        app.UseThemeMiddleware();

        // Lưu ý thứ tự: Authentication phải đặt trước Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
        });
        //InitializeDatabase(serviceProvider);

    }
    private void InitializeDatabase(IServiceProvider serviceProvider)
    {
        try
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<YourDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Startup>>();

                logger.LogInformation("Đang khởi tạo database...");

                // Tạo database nếu không tồn tại
                context.Database.EnsureCreated();

                // Thêm MangaDex Source nếu chưa có
                if (!context.Sources.Any(s => s.SourceName == "MangaDex"))
                {
                    context.Sources.Add(new Mangareading.Models.Source
                    {
                        SourceName = "MangaDex",
                        SourceUrl = "https://mangadex.org",
                        ApiBaseUrl = "https://api.mangadex.org",
                        IsActive = true
                    });

                    context.SaveChanges();
                    logger.LogInformation("Đã thêm MangaDex vào bảng Sources");
                }

                logger.LogInformation("Khởi tạo database thành công");
            }
        }
        catch (Exception ex)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Startup>>();
                logger.LogError(ex, "Lỗi khi khởi tạo database");
            }
        }
    }
}