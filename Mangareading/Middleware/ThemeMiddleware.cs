using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Mangareading.Models;
using System.Threading.Tasks;
using System.Security.Claims;

namespace Mangareading.Middleware
{
    public class ThemeMiddleware
    {
        private readonly RequestDelegate _next;

        public ThemeMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, YourDbContext dbContext)
        {
            // Only proceed if the user is authenticated
            if (context.User.Identity.IsAuthenticated)
            {
                // Check if theme preference is already in session
                if (!context.Session.Keys.Contains("UserTheme"))
                {
                    // Get the current user's username
                    var username = context.User.FindFirstValue(ClaimTypes.Name);
                    
                    if (!string.IsNullOrEmpty(username))
                    {
                        // Get the user's theme preference from the database
                        var user = await dbContext.Users
                            .AsNoTracking()
                            .FirstOrDefaultAsync(u => u.Username == username);
                        
                        if (user != null)
                        {
                            // Store theme preference in session
                            context.Session.SetString("UserTheme", user.ThemePreference);
                        }
                    }
                }
            }
            
            await _next(context);
        }
    }
    
    // Extension method for registering the middleware
    public static class ThemeMiddlewareExtensions
    {
        public static IApplicationBuilder UseThemeMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ThemeMiddleware>();
        }
    }
} 