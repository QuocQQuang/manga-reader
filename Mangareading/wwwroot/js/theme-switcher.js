/**
 * Theme Switcher
 * Handles theme switching functionality (light/dark mode)
 */

// Expose ThemeSwitcher object globally
window.ThemeSwitcher = (function() {
    // Initialize theme on page load
    document.addEventListener('DOMContentLoaded', function() {
        initializeTheme();

        // Add transition class after initial theme is applied
        // This prevents transition animation on first page load
        setTimeout(() => {
            document.body.classList.add('theme-transition');
        }, 100);
    });

    /**
     * Initialize theme based on user preference or system preference
     */
    function initializeTheme() {
        // Remove transition class to prevent animation on initial load
        document.body.classList.remove('theme-transition');

        // Check for saved theme preference
        const savedTheme = localStorage.getItem('theme');

        // If no saved preference, check system preference
        if (!savedTheme) {
            const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
            setTheme(prefersDark ? 'dark' : 'light', false);
        } else {
            setTheme(savedTheme, false);
        }

        // Set up theme toggle buttons
        setupThemeToggleButtons();

        // Listen for system preference changes
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
            if (!localStorage.getItem('theme')) {
                setTheme(e.matches ? 'dark' : 'light', true);
            }
        });
    }

    /**
     * Set the theme for the site
     * param {string} theme - The theme to set ('light' or 'dark')
     * param {boolean} useTransition - Whether to use transition animation
     */
    function setTheme(theme, useTransition = true) {
        console.log(`[ThemeSwitcher] setTheme called with: ${theme}, useTransition: ${useTransition}`); // Add log
        // Apply or remove transition class based on parameter
        if (useTransition) {
            document.body.classList.add('theme-transition');
        } else {
            document.body.classList.remove('theme-transition');
        }

        // Set theme attribute on HTML element
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem('theme', theme);

        // Update toggle button icons if they exist
        updateToggleIcons(theme);

        // Emit a custom event that other scripts can listen for
        console.log('[ThemeSwitcher] Dispatching themeChanged event:', theme); // Add log
        document.dispatchEvent(new CustomEvent('themeChanged', { 
            bubbles: true,
            detail: { theme: theme }
        }));
        
        // Also trigger jQuery event for older components (redundant if everything uses native event)
        // if (typeof jQuery !== 'undefined') {
        //     jQuery(document).trigger('themeChanged', theme);
        // }

        // If we used transition, remove the class after transition completes
        if (useTransition) {
            setTimeout(() => {
                document.body.classList.remove('theme-transition');
            }, 300); // Match this with your CSS transition duration
        }
    }

    /**
     * Set up theme toggle buttons
     */
    function setupThemeToggleButtons() {
        // Main navbar theme toggle (checkbox style)
        const themeToggleCheckbox = document.getElementById('themeToggleCheckbox');
        if (themeToggleCheckbox) {
            // Set initial state based on current theme
            const currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
            themeToggleCheckbox.checked = currentTheme === 'dark';

            // Add change event listener
            themeToggleCheckbox.addEventListener('change', function() {
                const newTheme = this.checked ? 'dark' : 'light';
                setTheme(newTheme, true); // Use transition when user clicks
            });
        }

        // Reading mode theme toggle buttons
        const themeToggleButtons = document.querySelectorAll('#themeToggleBtn, #themeToggleBtnBottom, #themeToggleBtnTop');
        console.log('[ThemeSwitcher] Found reading mode toggle buttons:', themeToggleButtons.length); // Add log
        themeToggleButtons.forEach(function(button) {
            if (button) {
                // Remove any existing listeners first to prevent duplicates
                button.removeEventListener('click', handleThemeButtonClick);
                // Add the new listener
                button.addEventListener('click', handleThemeButtonClick);
                console.log(`[ThemeSwitcher] Added click listener to button: #${button.id}`); // Add log
            }
        });
    }

    // Extracted handler function for reading buttons
    function handleThemeButtonClick() {
        console.log(`[ThemeSwitcher] Button clicked: #${this.id}`); // Add log
        const currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
        const newTheme = currentTheme === 'light' ? 'dark' : 'light';
        setTheme(newTheme, true); // Use transition when user clicks

        // Also update checkbox if it exists
        const checkbox = document.getElementById('themeToggleCheckbox');
        if (checkbox) {
            checkbox.checked = newTheme === 'dark';
        }
    }

    /**
     * Update toggle button icons based on current theme
     */
    function updateToggleIcons(theme) {
        // Update checkbox state
        const themeToggleCheckbox = document.getElementById('themeToggleCheckbox');
        if (themeToggleCheckbox) {
            themeToggleCheckbox.checked = theme === 'dark';
        }

        // Update reading mode toggle icons
        const themeToggleButtons = document.querySelectorAll('#themeToggleBtn, #themeToggleBtnBottom, #themeToggleBtnTop');
        themeToggleButtons.forEach(function(button) {
            if (button) {
                const icon = button.querySelector('i');
                if (icon) {
                    if (theme === 'dark') {
                        icon.classList.remove('fa-sun');
                        icon.classList.add('fa-moon');
                    } else {
                        icon.classList.remove('fa-moon');
                        icon.classList.add('fa-sun');
                    }
                }
            }
        });
    }

    // Public API for ThemeSwitcher
    return {
        initialize: initializeTheme,
        setTheme: setTheme // Expose setTheme function
    };
})();

// Initialize theme on page load
document.addEventListener('DOMContentLoaded', function() {
    window.ThemeSwitcher.initialize();
});
