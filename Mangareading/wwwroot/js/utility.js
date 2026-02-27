/**
 * Utility functions for the Manga Reader application
 */

// Initialize components when the DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    // Initialize tooltips
    initializeTooltips();
    
    // Initialize popovers
    initializePopovers();
    
    // Enhanced image error handling
    handleImageErrors();
    
    // Enhanced lazy loading of images
    lazyLoadImages();
    
    // Apply theme from user preference
    applyThemeFromPreference();
});

/**
 * Initialize Bootstrap tooltips
 */
function initializeTooltips() {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

/**
 * Initialize Bootstrap popovers
 */
function initializePopovers() {
    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    var popoverList = popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });
}

/**
 * Handle image errors by replacing with a fallback image
 */
function handleImageErrors() {
    document.querySelectorAll('img').forEach(function(img) {
        img.addEventListener('error', function() {
            // Only replace if not already replaced
            if (!this.classList.contains('error-replaced')) {
                this.setAttribute('src', '/images/no-cover.png');
                this.classList.add('error-replaced');
            }
        });
    });
}

/**
 * Lazy load images with fade-in effect using Intersection Observer
 */
function lazyLoadImages() {
    if ('IntersectionObserver' in window) {
        const imgObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    const src = img.getAttribute('data-src');

                    if (src) {
                        // Create animation by adding transition classes
                        img.classList.add('img-loading');

                        // Load image
                        img.setAttribute('src', src);
                        img.removeAttribute('data-src');

                        // Add loaded class when image is loaded
                        img.onload = () => {
                            img.classList.remove('img-loading');
                            img.classList.add('img-loaded');
                        };
                    }

                    observer.unobserve(img);
                }
            });
        }, {
            rootMargin: '200px',
            threshold: 0.1
        });

        document.querySelectorAll('img[data-src]').forEach(img => {
            imgObserver.observe(img);
        });
    } else {
        // Fallback for older browsers
        document.querySelectorAll('img[data-src]').forEach(img => {
            img.setAttribute('src', img.getAttribute('data-src'));
            img.removeAttribute('data-src');
        });
    }
}

/**
 * Show toast notification
 * param {string} message - Message to display in the toast
 * param {string} type - Type of toast (success, danger, warning, info)
 */
function showToast(message, type = 'info') {
    const toastContainer = document.getElementById('toastContainer');

    if (!toastContainer) {
        // Create toast container if not exists
        const container = document.createElement('div');
        container.id = 'toastContainer';
        container.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        container.style.zIndex = '1090';
        document.body.appendChild(container);
    }

    // Create unique ID for this toast
    const toastId = 'toast-' + Date.now();
    const progressId = 'progress-' + Date.now();

    // Define border class based on type
    const borderClass = `border border-${type}`;

    // Create toast HTML
    const toastHtml = `
        <div id="${toastId}" class="toast ${borderClass} bg-${type} position-relative overflow-hidden" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="toast-header">
                <i class="fas ${getIconForType(type)} me-2"></i>
                <strong class="me-auto">MangaReader</strong>
                <small>${getCurrentTime()}</small>
                <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
            <div class="toast-body text-white">
                ${message}
            </div>
            <div id="${progressId}" class="toast-progress-bar" style="background-color: rgba(255,255,255,0.3); height: 3px; width: 100%; position: absolute; bottom: 0; left: 0;"></div>
        </div>
    `;

    // Add toast to container
    document.getElementById('toastContainer').innerHTML += toastHtml;

    // Initialize and show the toast
    const toastElement = document.getElementById(toastId);
    const progressElement = document.getElementById(progressId);
    
    // Standard time for toast to be shown (in ms)
    const delay = 5000;
    
    const toast = new bootstrap.Toast(toastElement, {
        delay: delay,
        animation: true
    });

    toast.show();
    
    // Animate the progress bar
    const startTime = Date.now();
    const animateProgress = () => {
        const elapsed = Date.now() - startTime;
        const remaining = Math.max(0, 1 - elapsed / delay);
        
        if (progressElement) {
            progressElement.style.width = `${remaining * 100}%`;
        }
        
        if (remaining > 0 && toastElement.parentElement) {
            requestAnimationFrame(animateProgress);
        }
    };
    
    requestAnimationFrame(animateProgress);

    // Remove toast element after it's hidden
    toastElement.addEventListener('hidden.bs.toast', () => {
        toastElement.remove();
    });
}

/**
 * Get appropriate icon for toast type
 * param {string} type - Type of toast
 * @returns {string} - Font Awesome icon class
 */
function getIconForType(type) {
    switch (type) {
        case 'success': return 'fa-check-circle';
        case 'danger': return 'fa-exclamation-circle';
        case 'warning': return 'fa-exclamation-triangle';
        case 'info':
        default: return 'fa-info-circle';
    }
}

/**
 * Get current time formatted for toast
 * @returns {string} - Formatted time string
 */
function getCurrentTime() {
    const now = new Date();
    return now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

/**
 * Toggle between light and dark mode
 * param {boolean} savePreference - Whether to save the preference to localStorage
 */
function toggleTheme(savePreference = true) {
    // Add transition class for smooth color change
    document.body.classList.add('theme-transition');

    // Toggle theme attribute
    if (document.documentElement.getAttribute('data-theme') === 'dark') {
        document.documentElement.setAttribute('data-theme', 'light');
        updateThemeToggleButton('light');
        if (savePreference) {
            localStorage.setItem('theme', 'light');
            saveUserThemePreference('light');
        }
    } else {
        document.documentElement.setAttribute('data-theme', 'dark');
        updateThemeToggleButton('dark');
        if (savePreference) {
            localStorage.setItem('theme', 'dark');
            saveUserThemePreference('dark');
        }
    }

    // Remove transition class after transition completes
    setTimeout(() => {
        document.body.classList.remove('theme-transition');
    }, 300);
}

/**
 * Update the theme toggle button icon and text
 * param {string} theme - Current theme ('light' or 'dark')
 */
function updateThemeToggleButton(theme) {
    const themeToggleBtn = document.getElementById('themeToggleBtn');
    if (themeToggleBtn) {
        const icon = themeToggleBtn.querySelector('i');
        if (theme === 'dark') {
            icon.classList.remove('fa-moon');
            icon.classList.add('fa-sun');
            themeToggleBtn.setAttribute('title', 'Chuyển sang chế độ sáng');
        } else {
            icon.classList.remove('fa-sun');
            icon.classList.add('fa-moon');
            themeToggleBtn.setAttribute('title', 'Chuyển sang chế độ tối');
        }
    }
}

/**
 * Apply theme from user preference (localStorage or system preference)
 */
function applyThemeFromPreference() {
    // First check localStorage
    const savedTheme = localStorage.getItem('theme');
    
    // Then check system preference
    const prefersDarkScheme = window.matchMedia('(prefers-color-scheme: dark)');
    
    if (savedTheme === 'dark' || (!savedTheme && prefersDarkScheme.matches)) {
        document.documentElement.setAttribute('data-theme', 'dark');
        updateThemeToggleButton('dark');
    } else {
        document.documentElement.setAttribute('data-theme', 'light');
        updateThemeToggleButton('light');
    }
    
    // Listen for system theme changes
    prefersDarkScheme.addEventListener('change', (e) => {
        if (!localStorage.getItem('theme')) {
            if (e.matches) {
                document.documentElement.setAttribute('data-theme', 'dark');
                updateThemeToggleButton('dark');
            } else {
                document.documentElement.setAttribute('data-theme', 'light');
                updateThemeToggleButton('light');
            }
        }
    });
}

/**
 * Save user theme preference to the server
 * param {string} theme - Theme to save ('light' or 'dark')
 */
function saveUserThemePreference(theme) {
    // Only save if user is logged in
    if (isUserLoggedIn()) {
        fetch('/User/SaveThemePreference', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            },
            body: JSON.stringify({ theme: theme })
        })
        .then(response => {
            // Theme preference saved successfully, no need to do anything
        })
        .catch(error => {
            console.error('Error saving theme preference:', error);
        });
    }
}

/**
 * Check if the user is currently logged in
 * @returns {boolean} - Whether the user is logged in
 */
function isUserLoggedIn() {
    // Check if there's a user dropdown in the header
    return document.getElementById('userDropdown') !== null;
}

// Export functions for global use
window.showToast = showToast;
window.toggleTheme = toggleTheme;