using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Mangareading.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Add security headers to improve HTTPS and cookie security
            
            // Strict-Transport-Security: instructs browsers to only use HTTPS
            // max-age=31536000 (1 year)
            // includeSubDomains ensures all subdomains are also covered
            context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
            
            // X-Content-Type-Options: prevents browsers from MIME-sniffing 
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            
            // X-XSS-Protection: helps prevent cross-site scripting attacks
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            
            // Content-Security-Policy: helps prevent multiple types of attacks
            // Only in production mode or with a valid certificate should this be enabled fully
            if (!context.Request.Host.Host.Contains("localhost") && !context.Request.Host.Host.Contains("127.0.0.1"))
            {
                context.Response.Headers.Add("Content-Security-Policy", 
                    "default-src 'self' https: data:; " +
                    "script-src 'self' https: 'unsafe-inline' 'unsafe-eval'; " +
                    "style-src 'self' https: 'unsafe-inline'; " +
                    "img-src 'self' https: data:; " +
                    "font-src 'self' https: data:; " +
                    "connect-src 'self' https:; " +
                    "frame-src 'self' https:;");
            }
            
            // Referrer-Policy: controls how much referrer information is included
            context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

            // Permission-Policy (formerly Feature-Policy): restricts which browser features the site can use
            context.Response.Headers.Add("Permissions-Policy", 
                "camera=(), microphone=(), geolocation=(), payment=()");

            await _next(context);
        }
    }
}