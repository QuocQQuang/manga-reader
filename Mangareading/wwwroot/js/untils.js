

// Namespace for utility functions
window.Utils = {
    /**
     * Format date time string to locale format
     * param {string} dateTimeString - Date time string in format YYYY-MM-DD HH:MM:SS
     * param {string} locale - Locale for formatting (default: vi-VN)
     * @returns {string} Formatted date time string
     */
    formatDateTime: function (dateTimeString, locale = 'vi-VN') {
        try {
            const parts = dateTimeString.split(' ');
            const datePart = parts[0];
            const timePart = parts[1] || '00:00:00';

            const [year, month, day] = datePart.split('-');
            const [hour, minute, second] = timePart.split(':');

            const date = new Date(year, month - 1, day, hour, minute, second);

            return date.toLocaleString(locale, {
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit'
            });
        } catch (e) {
            console.error('Error formatting date time:', e);
            return dateTimeString;
        }
    },

    /**
     * Get relative time from now
     * param {string} dateTimeString - Date time string in format YYYY-MM-DD HH:MM:SS
     * @returns {string} Relative time string
     */
    getRelativeTime: function (dateTimeString) {
        try {
            const parts = dateTimeString.split(' ');
            const datePart = parts[0];
            const timePart = parts[1] || '00:00:00';

            const [year, month, day] = datePart.split('-');
            const [hour, minute, second] = timePart.split(':');

            const date = new Date(year, month - 1, day, hour, minute, second);
            const now = new Date();

            const diffMs = now - date;
            const diffSec = Math.floor(diffMs / 1000);
            const diffMin = Math.floor(diffSec / 60);
            const diffHour = Math.floor(diffMin / 60);
            const diffDay = Math.floor(diffHour / 24);
            const diffMonth = Math.floor(diffDay / 30);
            const diffYear = Math.floor(diffMonth / 12);

            if (diffYear > 0) {
                return `${diffYear} năm trước`;
            } else if (diffMonth > 0) {
                return `${diffMonth} tháng trước`;
            } else if (diffDay > 0) {
                return `${diffDay} ngày trước`;
            } else if (diffHour > 0) {
                return `${diffHour} giờ trước`;
            } else if (diffMin > 0) {
                return `${diffMin} phút trước`;
            } else {
                return 'Vừa xong';
            }
        } catch (e) {
            console.error('Error calculating relative time:', e);
            return 'Không xác định';
        }
    },

    /**
     * Check image URL for potential loading issues
     * param {string} url - Image URL to check
     * @returns {Promise<boolean>} True if image seems valid, false otherwise
     */
    validateImageUrl: async function (url) {
        try {
            // Check if URL has valid image extension
            const validExtensions = ['.jpg', '.jpeg', '.png', '.webp', '.gif'];
            const hasValidExtension = validExtensions.some(ext =>
                url.toLowerCase().includes(ext)
            );

            if (!hasValidExtension) {
                return false;
            }

            // Try to fetch image headers to check validity
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), 5000);

            const response = await fetch(url, {
                method: 'HEAD',
                mode: 'no-cors',
                signal: controller.signal
            });

            clearTimeout(timeoutId);

            // If we got here, the request didn't throw an error
            return true;
        } catch (e) {
            console.warn('Image URL validation failed:', e);
            return false;
        }
    },

    /**
     * Get alternative image sources for a failed image
     * param {string} originalUrl - Original image URL that failed
     * @returns {Array} Array of alternative URLs to try
     */
    getAlternativeImageSources: function (originalUrl) {
        const alternatives = [];

        // Try CDN alternatives
        if (originalUrl.includes('uploads.mangadex.org')) {
            alternatives.push(originalUrl.replace('uploads.mangadex.org', 'mangadex-images.b-cdn.net'));
        }

        // Try with different protocol
        if (originalUrl.startsWith('https:')) {
            alternatives.push(originalUrl.replace('https:', 'http:'));
        } else if (originalUrl.startsWith('http:')) {
            alternatives.push(originalUrl.replace('http:', 'https:'));
        }

        // Try with query parameters to bypass cache
        alternatives.push(originalUrl + (originalUrl.includes('?') ? '&' : '?') + 'bypass=' + Date.now());

        return alternatives;
    },

    /**
     * Update all timestamps on the page to show relative time
     * param {string} selector - CSS selector for timestamp elements
     * param {string} dataAttribute - Data attribute containing the timestamp
     */
    updateAllTimestamps: function (selector = '.timestamp', dataAttribute = 'data-time') {
        document.querySelectorAll(selector).forEach(el => {
            const timestamp = el.getAttribute(dataAttribute);
            if (timestamp) {
                el.textContent = this.getRelativeTime(timestamp);
                el.title = this.formatDateTime(timestamp);
            }
        });
    },

    /**
     * Check current user connection quality
     * @returns {Promise<Object>} Connection quality information
     */
    checkConnectionQuality: async function () {
        try {
            const startTime = performance.now();
            const response = await fetch('/api/ping', {
                method: 'GET',
                cache: 'no-store'
            });
            const endTime = performance.now();

            const latency = Math.round(endTime - startTime);

            let quality = 'unknown';
            if (latency < 100) {
                quality = 'excellent';
            } else if (latency < 300) {
                quality = 'good';
            } else if (latency < 600) {
                quality = 'average';
            } else {
                quality = 'poor';
            }

            const connection = navigator.connection ||
                navigator.mozConnection ||
                navigator.webkitConnection || {};

            return {
                latency,
                quality,
                effectiveType: connection.effectiveType || 'unknown',
                downlink: connection.downlink || 'unknown',
                rtt: connection.rtt || 'unknown',
                online: navigator.onLine
            };
        } catch (e) {
            console.warn('Failed to check connection quality:', e);
            return {
                latency: -1,
                quality: 'error',
                effectiveType: 'unknown',
                online: navigator.onLine
            };
        }
    }
};

// Automatically update timestamps when document loads
document.addEventListener('DOMContentLoaded', function () {
    Utils.updateAllTimestamps();

    // Update timestamps every minute
    setInterval(() => {
        Utils.updateAllTimestamps();
    }, 60000);

    // Update current user and time display
    const currentDateTime = document.getElementById('currentDateTime')?.value || "2025-03-10 13:55:57";
    const currentUser = document.getElementById('currentUser')?.value || "QuocQQuangtiep";

    document.querySelectorAll('.timestamp-display span').forEach(el => {
        el.textContent = currentDateTime;
        el.title = Utils.formatDateTime(currentDateTime);
    });

    document.querySelectorAll('.user-badge span').forEach(el => {
        el.textContent = currentUser;
    });

    // Update footer info
    const updateInfoEl = document.getElementById('updateInfo');
    if (updateInfoEl) {
        updateInfoEl.innerHTML = `<div class="container">
            <span>MangaReader v2.1.0 | Cập nhật: ${currentDateTime} | ${currentUser}</span>
        </div>`;
    }
});