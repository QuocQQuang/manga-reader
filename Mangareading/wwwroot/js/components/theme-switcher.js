/**
 * Theme Switcher Component
 * Handles theme switching functionality for the Manga Reader application
 */

class ThemeSwitcher {
    /**
     * Initialize a new ThemeSwitcher instance
     * param {Object} options - Configuration options
     * param {string} options.toggleButtonId - ID of the toggle button element
     * param {string} options.storageKey - LocalStorage key for theme preference
     * param {boolean} options.useSystemPreference - Whether to use system preference as default
     * param {boolean} options.saveToServer - Whether to save preference to server
     * param {string} options.saveEndpoint - Endpoint for saving preference to server
     */
    constructor(options) {
        this.options = Object.assign({
            toggleButtonId: 'themeToggleBtn',
            storageKey: 'theme',
            useSystemPreference: true,
            saveToServer: true,
            saveEndpoint: '/User/SaveThemePreference'
        }, options);

        this.toggleButton = document.getElementById(this.options.toggleButtonId);
        this.currentTheme = 'light';
        
        this.init();
    }
    
    /**
     * Initialize the theme switcher
     */
    init() {
        // Apply theme from saved preference or system preference
        this.applyThemeFromPreference();
        
        // Add event listener to toggle button if it exists
        if (this.toggleButton) {
            this.toggleButton.addEventListener('click', this.toggleTheme.bind(this));
        }
        
        // Listen for system theme changes if enabled
        if (this.options.useSystemPreference) {
            this.listenForSystemThemeChanges();
        }
    }
    
    /**
     * Apply theme from saved preference or system preference
     */
    applyThemeFromPreference() {
        // First check localStorage
        const savedTheme = localStorage.getItem(this.options.storageKey);
        
        // Then check system preference if enabled and no saved preference
        if (!savedTheme && this.options.useSystemPreference) {
            const prefersDarkScheme = window.matchMedia('(prefers-color-scheme: dark)');
            
            if (prefersDarkScheme.matches) {
                this.setTheme('dark', false);
            } else {
                this.setTheme('light', false);
            }
        } else if (savedTheme) {
            this.setTheme(savedTheme, false);
        }
    }
    
    /**
     * Listen for system theme changes
     */
    listenForSystemThemeChanges() {
        const prefersDarkScheme = window.matchMedia('(prefers-color-scheme: dark)');
        
        prefersDarkScheme.addEventListener('change', (e) => {
            // Only apply system preference if no saved preference
            if (!localStorage.getItem(this.options.storageKey)) {
                if (e.matches) {
                    this.setTheme('dark', false);
                } else {
                    this.setTheme('light', false);
                }
            }
        });
    }
    
    /**
     * Toggle between light and dark theme
     */
    toggleTheme() {
        const newTheme = this.currentTheme === 'dark' ? 'light' : 'dark';
        this.setTheme(newTheme, true);
    }
    
    /**
     * Set theme to light or dark
     * param {string} theme - Theme to set ('light' or 'dark')
     * param {boolean} savePreference - Whether to save the preference
     */
    setTheme(theme, savePreference = true) {
        // Add transition class for smooth color change
        document.body.classList.add('theme-transition');
        
        // Set theme attribute on html element
        document.documentElement.setAttribute('data-theme', theme);
        
        // Update current theme
        this.currentTheme = theme;
        
        // Update toggle button if it exists
        this.updateToggleButton();
        
        // Save preference if enabled
        if (savePreference) {
            this.saveThemePreference();
        }
        
        // Remove transition class after transition completes
        setTimeout(() => {
            document.body.classList.remove('theme-transition');
        }, 300);
    }
    
    /**
     * Update toggle button appearance
     */
    updateToggleButton() {
        if (!this.toggleButton) return;
        
        const icon = this.toggleButton.querySelector('i');
        
        if (!icon) return;
        
        if (this.currentTheme === 'dark') {
            icon.classList.remove('fa-moon');
            icon.classList.add('fa-sun');
            this.toggleButton.setAttribute('title', 'Chuyển sang chế độ sáng');
        } else {
            icon.classList.remove('fa-sun');
            icon.classList.add('fa-moon');
            this.toggleButton.setAttribute('title', 'Chuyển sang chế độ tối');
        }
    }
    
    /**
     * Save theme preference to localStorage and server
     */
    saveThemePreference() {
        // Save to localStorage
        localStorage.setItem(this.options.storageKey, this.currentTheme);
        
        // Save to server if enabled and user is logged in
        if (this.options.saveToServer && this.isUserLoggedIn()) {
            this.saveThemePreferenceToServer();
        }
    }
    
    /**
     * Save theme preference to server
     */
    saveThemePreferenceToServer() {
        // Get CSRF token if available
        const csrfToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        
        // Send request to server
        fetch(this.options.saveEndpoint, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': csrfToken || ''
            },
            body: JSON.stringify({ theme: this.currentTheme })
        })
        .then(response => {
            if (!response.ok) {
                throw new Error('Failed to save theme preference');
            }
            return response.json();
        })
        .then(data => {
            // Theme preference saved successfully
            console.log('Theme preference saved to server');
        })
        .catch(error => {
            console.error('Error saving theme preference:', error);
        });
    }
    
    /**
     * Check if the user is currently logged in
     * @returns {boolean} Whether the user is logged in
     */
    isUserLoggedIn() {
        // Check if there's a user dropdown in the header
        return document.getElementById('userDropdown') !== null;
    }
    
    /**
     * Get current theme
     * @returns {string} Current theme ('light' or 'dark')
     */
    getTheme() {
        return this.currentTheme;
    }
}

// Create global theme switcher instance
const themeSwitcher = new ThemeSwitcher();

// Export the ThemeSwitcher class and global instance
window.ThemeSwitcher = ThemeSwitcher;
window.themeSwitcher = themeSwitcher;

// Backward compatibility with existing code
window.toggleTheme = function(savePreference = true) {
    themeSwitcher.toggleTheme();
};
