/**
 * Date Formatter Utility
 * Provides date formatting utilities for the Manga Reader application
 */

class DateFormatter {
    /**
     * Get relative time string for a timestamp
     * param {Date|string|number} date - Date object, ISO string, or timestamp
     * @returns {string} - Relative time string
     */
    static getRelativeTime(date) {
        if (!(date instanceof Date)) {
            date = new Date(date);
        }
        
        const now = new Date();
        const diffMs = now - date;
        const diffSec = Math.floor(diffMs / 1000);
        const diffMin = Math.floor(diffSec / 60);
        const diffHour = Math.floor(diffMin / 60);
        const diffDay = Math.floor(diffHour / 24);
        const diffMonth = Math.floor(diffDay / 30);
        const diffYear = Math.floor(diffDay / 365);
        
        if (diffSec < 60) {
            return 'vừa xong';
        } else if (diffMin < 60) {
            return `${diffMin} phút trước`;
        } else if (diffHour < 24) {
            return `${diffHour} giờ trước`;
        } else if (diffDay < 30) {
            return `${diffDay} ngày trước`;
        } else if (diffMonth < 12) {
            return `${diffMonth} tháng trước`;
        } else {
            return `${diffYear} năm trước`;
        }
    }
    
    /**
     * Format date to locale string
     * param {Date|string|number} date - Date object, ISO string, or timestamp
     * param {Object} options - Intl.DateTimeFormat options
     * @returns {string} - Formatted date string
     */
    static formatDate(date, options = {}) {
        if (!(date instanceof Date)) {
            date = new Date(date);
        }
        
        const defaultOptions = {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit'
        };
        
        const mergedOptions = { ...defaultOptions, ...options };
        
        return date.toLocaleDateString('vi-VN', mergedOptions);
    }
    
    /**
     * Format time to locale string
     * param {Date|string|number} date - Date object, ISO string, or timestamp
     * param {Object} options - Intl.DateTimeFormat options
     * @returns {string} - Formatted time string
     */
    static formatTime(date, options = {}) {
        if (!(date instanceof Date)) {
            date = new Date(date);
        }
        
        const defaultOptions = {
            hour: '2-digit',
            minute: '2-digit'
        };
        
        const mergedOptions = { ...defaultOptions, ...options };
        
        return date.toLocaleTimeString('vi-VN', mergedOptions);
    }
    
    /**
     * Format date and time to locale string
     * param {Date|string|number} date - Date object, ISO string, or timestamp
     * param {Object} options - Intl.DateTimeFormat options
     * @returns {string} - Formatted date and time string
     */
    static formatDateTime(date, options = {}) {
        if (!(date instanceof Date)) {
            date = new Date(date);
        }
        
        const defaultOptions = {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit'
        };
        
        const mergedOptions = { ...defaultOptions, ...options };
        
        return date.toLocaleString('vi-VN', mergedOptions);
    }
    
    /**
     * Update all elements with relative time
     * param {string} selector - Selector for elements to update
     * param {string} dateAttribute - Data attribute containing the date
     */
    static updateRelativeTimes(selector = '[data-time]', dateAttribute = 'data-time') {
        document.querySelectorAll(selector).forEach(element => {
            const timestamp = element.getAttribute(dateAttribute);
            if (timestamp) {
                element.textContent = DateFormatter.getRelativeTime(timestamp);
            }
        });
    }
}

// Export the DateFormatter class
window.DateFormatter = DateFormatter;

// Backward compatibility with existing code
window.getRelativeTime = DateFormatter.getRelativeTime;

// Set up automatic updating of relative times
document.addEventListener('DOMContentLoaded', function() {
    // Update relative times on page load
    DateFormatter.updateRelativeTimes();
    
    // Update relative times every minute
    setInterval(() => {
        DateFormatter.updateRelativeTimes();
    }, 60000);
});
