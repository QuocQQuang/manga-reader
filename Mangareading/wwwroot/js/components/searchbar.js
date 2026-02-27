/**
 * Searchbar Component
 * Reusable searchbar component for the Manga Reader application
 */

class SearchBar {
    /**
     * Initialize a new SearchBar instance
     * param {Object} options - Configuration options
     * param {string} options.containerId - ID of the container element
     * param {string} options.inputId - ID of the input element
     * param {string} options.resultsId - ID of the results container element
     * param {string} options.endpoint - API endpoint for search
     * param {number} options.minChars - Minimum characters to trigger search
     * param {number} options.debounceTime - Debounce time in milliseconds
     * param {Function} options.onSelect - Callback when an item is selected
     */
    constructor(options) {
        this.options = Object.assign({
            containerId: 'searchbar-container',
            inputId: 'searchbar-input',
            resultsId: 'searchbar-results',
            endpoint: '/search/autocomplete',
            minChars: 2,
            debounceTime: 300,
            onSelect: null
        }, options);

        this.container = document.getElementById(this.options.containerId);
        this.input = document.getElementById(this.options.inputId);
        this.results = document.getElementById(this.options.resultsId);
        
        this.searchTimer = null;
        this.isLoading = false;
        
        this.init();
    }
    
    /**
     * Initialize the searchbar
     */
    init() {
        if (!this.container || !this.input || !this.results) {
            console.error('SearchBar: Required elements not found');
            return;
        }
        
        // Add event listeners
        this.input.addEventListener('input', this.handleInput.bind(this));
        this.input.addEventListener('focus', this.handleFocus.bind(this));
        
        // Close results when clicking outside
        document.addEventListener('click', (e) => {
            if (!this.container.contains(e.target)) {
                this.hideResults();
            }
        });
        
        // Keyboard navigation
        this.input.addEventListener('keydown', this.handleKeydown.bind(this));
    }
    
    /**
     * Handle input event
     * param {Event} e - Input event
     */
    handleInput(e) {
        const query = this.input.value.trim();
        
        clearTimeout(this.searchTimer);
        
        if (query.length < this.options.minChars) {
            this.hideResults();
            return;
        }
        
        this.searchTimer = setTimeout(() => {
            this.fetchResults(query);
        }, this.options.debounceTime);
    }
    
    /**
     * Handle focus event
     * param {Event} e - Focus event
     */
    handleFocus(e) {
        const query = this.input.value.trim();
        
        if (query.length >= this.options.minChars) {
            this.fetchResults(query);
        }
    }
    
    /**
     * Handle keydown event for keyboard navigation
     * param {KeyboardEvent} e - Keydown event
     */
    handleKeydown(e) {
        if (!this.results.classList.contains('show')) {
            return;
        }
        
        const items = this.results.querySelectorAll('.searchbar-result-item');
        const activeItem = this.results.querySelector('.searchbar-result-item.active');
        let index = -1;
        
        if (activeItem) {
            index = Array.from(items).indexOf(activeItem);
        }
        
        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                this.setActiveItem(items, index + 1);
                break;
                
            case 'ArrowUp':
                e.preventDefault();
                this.setActiveItem(items, index - 1);
                break;
                
            case 'Enter':
                e.preventDefault();
                if (activeItem) {
                    activeItem.click();
                }
                break;
                
            case 'Escape':
                e.preventDefault();
                this.hideResults();
                break;
        }
    }
    
    /**
     * Set active item for keyboard navigation
     * param {NodeList} items - List of result items
     * param {number} index - Index of item to activate
     */
    setActiveItem(items, index) {
        if (items.length === 0) return;
        
        // Remove active class from all items
        items.forEach(item => item.classList.remove('active'));
        
        // Handle wrapping
        if (index < 0) index = items.length - 1;
        if (index >= items.length) index = 0;
        
        // Add active class to new item
        items[index].classList.add('active');
        
        // Scroll into view if needed
        items[index].scrollIntoView({ block: 'nearest' });
    }
    
    /**
     * Fetch search results from the server
     * param {string} query - Search query
     */
    fetchResults(query) {
        if (this.isLoading) return;
        
        this.isLoading = true;
        this.showLoading();
        
        fetch(`${this.options.endpoint}?term=${encodeURIComponent(query)}`)
            .then(response => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                return response.json();
            })
            .then(data => {
                this.displayResults(data);
            })
            .catch(error => {
                console.error('Search error:', error);
                this.showError('Failed to fetch results');
            })
            .finally(() => {
                this.isLoading = false;
            });
    }
    
    /**
     * Display search results
     * param {Array} results - Search results
     */
    displayResults(results) {
        this.results.innerHTML = '';
        
        if (results.length === 0) {
            this.showNoResults();
            return;
        }
        
        results.forEach(item => {
            const resultItem = document.createElement('div');
            resultItem.className = 'searchbar-result-item';
            
            // Create result item content based on item type
            if (item.coverUrl) {
                // Item with image
                resultItem.innerHTML = `
                    <img src="${item.coverUrl || '/images/no-cover.png'}" alt="${item.title}" class="searchbar-result-image" onerror="this.src='/images/no-cover.png';">
                    <div class="searchbar-result-content">
                        <div class="searchbar-result-title">${item.title}</div>
                        <div class="searchbar-result-info">${item.author || ''}</div>
                    </div>
                `;
            } else {
                // Simple item
                resultItem.textContent = item.title || item.value;
            }
            
            // Add click event
            resultItem.addEventListener('click', () => {
                this.selectItem(item);
            });
            
            this.results.appendChild(resultItem);
        });
        
        // Add advanced search link
        const advancedLink = document.createElement('a');
        advancedLink.href = '/search/advanced';
        advancedLink.className = 'searchbar-advanced-link';
        advancedLink.textContent = 'Tìm kiếm nâng cao';
        this.results.appendChild(advancedLink);
        
        this.showResults();
    }
    
    /**
     * Show loading indicator
     */
    showLoading() {
        this.results.innerHTML = `
            <div class="searchbar-loading">
                <span class="searchbar-loading-spinner"></span>
                Đang tìm kiếm...
            </div>
        `;
        this.showResults();
    }
    
    /**
     * Show error message
     * param {string} message - Error message
     */
    showError(message) {
        this.results.innerHTML = `
            <div class="searchbar-no-results">
                <i class="fas fa-exclamation-circle"></i> ${message}
            </div>
        `;
        this.showResults();
    }
    
    /**
     * Show no results message
     */
    showNoResults() {
        this.results.innerHTML = `
            <div class="searchbar-no-results">
                Không tìm thấy kết quả
            </div>
            <a href="/search/advanced" class="searchbar-advanced-link">
                Tìm kiếm nâng cao
            </a>
        `;
        this.showResults();
    }
    
    /**
     * Show results container
     */
    showResults() {
        this.results.classList.add('show');
    }
    
    /**
     * Hide results container
     */
    hideResults() {
        this.results.classList.remove('show');
    }
    
    /**
     * Handle item selection
     * param {Object} item - Selected item
     */
    selectItem(item) {
        // Clear input and hide results
        this.input.value = item.title || item.value;
        this.hideResults();
        
        // Call onSelect callback if provided
        if (typeof this.options.onSelect === 'function') {
            this.options.onSelect(item);
        } else {
            // Default behavior: navigate to item URL
            if (item.url) {
                window.location.href = item.url;
            } else if (item.id) {
                window.location.href = `/manga/${item.id}`;
            }
        }
    }
}

// Export the SearchBar class
window.SearchBar = SearchBar;

// Initialize header search when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    // Initialize header search if elements exist
    const headerSearchInput = document.getElementById('headerSearchInput');
    const headerSearchResults = document.getElementById('headerSearchResults');
    
    if (headerSearchInput && headerSearchResults) {
        new SearchBar({
            containerId: 'headerSearch',
            inputId: 'headerSearchInput',
            resultsId: 'headerSearchResults',
            endpoint: '/search/autocomplete',
            minChars: 2,
            debounceTime: 300
        });
    }
});
