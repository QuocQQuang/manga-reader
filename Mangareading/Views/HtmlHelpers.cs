using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;

namespace Mangareading.Helpers
{
    public static class HtmlHelpers
    {
        /// <summary>
        /// Returns "active" if the specified controller and optional action matches the current request
        /// </summary>
        public static string IsActive(this IHtmlHelper htmlHelper, string controller, string action = null)
        {
            var routeData = htmlHelper.ViewContext.RouteData;
            
            var routeController = routeData.Values["controller"]?.ToString();
            var routeAction = routeData.Values["action"]?.ToString();
            
            var isActive = string.Equals(controller, routeController, StringComparison.OrdinalIgnoreCase);
            
            if (action != null)
            {
                isActive = isActive && string.Equals(action, routeAction, StringComparison.OrdinalIgnoreCase);
            }
            
            return isActive ? "active" : "";
        }
        
        /// <summary>
        /// Formats a date in relative time (e.g. "2 hours ago")
        /// </summary>
        public static string RelativeTime(this IHtmlHelper htmlHelper, DateTimeOffset date)
        {
            var timeSpan = DateTimeOffset.Now - date;
            
            if (timeSpan.TotalMinutes < 1)
                return "vừa xong";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} phút trước";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} giờ trước";
            if (timeSpan.TotalDays < 30)
                return $"{(int)timeSpan.TotalDays} ngày trước";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} tháng trước";
            
            return $"{(int)(timeSpan.TotalDays / 365)} năm trước";
        }
        
        /// <summary>
        /// Returns a CSS class for a manga status
        /// </summary>
        public static string StatusClass(this IHtmlHelper htmlHelper, string status)
        {
            return status?.ToLower() switch
            {
                "ongoing" => "status-ongoing",
                "completed" => "status-completed",
                "hiatus" => "status-hiatus",
                "canceled" => "status-canceled",
                _ => "status-ongoing"
            };
        }
    }
} 