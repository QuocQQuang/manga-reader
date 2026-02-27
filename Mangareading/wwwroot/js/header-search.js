/**
 * Header Search Functionality
 * Handles the search functionality in the header of the site
 */

document.addEventListener('DOMContentLoaded', function() {
    // Get elements
    const searchInput = document.getElementById('headerSearchInput');
    const searchResults = document.getElementById('headerSearchResults');
    
    if (!searchInput || !searchResults) {
        console.error('Header search elements not found');
        return;
    }
    
    let searchTimer = null;
    const debounceTime = 300; // ms
    const minChars = 2;
    
    // Add event listeners
    searchInput.addEventListener('input', handleSearchInput);
    searchInput.addEventListener('focus', handleSearchFocus);
    
    // Close results when clicking outside
    document.addEventListener('click', function(e) {
        if (!e.target.closest('.header-search')) {
            hideSearchResults();
        }
    });
    
    // Keyboard navigation
    searchInput.addEventListener('keydown', handleKeydown);
    
    /**
     * Handle input event
     */
    function handleSearchInput() {
        const query = searchInput.value.trim();
        
        clearTimeout(searchTimer);
        
        if (query.length < minChars) {
            hideSearchResults();
            return;
        }
        
        searchTimer = setTimeout(function() {
            fetchSearchResults(query);
        }, debounceTime);
    }
    
    /**
     * Handle focus event
     */
    function handleSearchFocus() {
        const query = searchInput.value.trim();
        
        if (query.length >= minChars) {
            fetchSearchResults(query);
        }
    }
    
    /**
     * Handle keydown event for keyboard navigation
     */
    function handleKeydown(e) {
        if (!searchResults.classList.contains('show')) {
            return;
        }
        
        const items = searchResults.querySelectorAll('.search-result-item');
        const activeItem = searchResults.querySelector('.search-result-item.active');
        let index = -1;
        
        if (activeItem) {
            index = Array.from(items).indexOf(activeItem);
        }
        
        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                setActiveItem(items, index + 1);
                break;
                
            case 'ArrowUp':
                e.preventDefault();
                setActiveItem(items, index - 1);
                break;
                
            case 'Enter':
                e.preventDefault();
                if (activeItem) {
                    activeItem.click();
                } else if (searchInput.value.trim().length > 0) {
                    // If no active item but search has text, submit search
                    window.location.href = '/search/results?query=' + encodeURIComponent(searchInput.value.trim());
                }
                break;
                
            case 'Escape':
                e.preventDefault();
                hideSearchResults();
                break;
        }
    }
    
    /**
     * Set active item for keyboard navigation
     */
    function setActiveItem(items, index) {
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
     */
    function fetchSearchResults(query) {
        // Show loading indicator
        searchResults.innerHTML = '<div class="search-loading">Đang tìm kiếm...</div>';
        showSearchResults();
        
        fetch('/search/autocomplete?term=' + encodeURIComponent(query))
            .then(response => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                return response.json();
            })
            .then(data => {
                displaySearchResults(data);
            })
            .catch(error => {
                console.error('Search error:', error);
                searchResults.innerHTML = '<div class="search-error">Lỗi khi tìm kiếm</div>';
            });
    }
    
    /**
     * Display search results
     */
    function displaySearchResults(results) {
        searchResults.innerHTML = '';
        
        if (results.length === 0) {
            searchResults.innerHTML = '<div class="search-no-results">Không tìm thấy kết quả</div>';
            return;
        }
        
        results.forEach(item => {
            const resultItem = document.createElement('div');
            resultItem.className = 'search-result-item';
            
            // Create result item content
            resultItem.innerHTML = `
                <div class="search-result-image">
                    <img src="${item.coverUrl || '/images/no-cover.png'}" alt="${item.value}" onerror="this.src='/images/no-cover.png';">
                </div>
                <div class="search-result-info">
                    <div class="search-result-title">${item.value}</div>
                    <div class="search-result-meta">${item.author || ''}</div>
                </div>
            `;
            
            // Add click event
            resultItem.addEventListener('click', function() {
                window.location.href = '/manga/' + item.id;
            });
            
            searchResults.appendChild(resultItem);
        });
        
        // Add advanced search link
        const advancedLink = document.createElement('a');
        advancedLink.href = '/search/advanced';
        advancedLink.className = 'search-advanced-link';
        advancedLink.textContent = 'Tìm kiếm nâng cao';
        searchResults.appendChild(advancedLink);
        
        showSearchResults();
    }
    
    /**
     * Show search results
     */
    function showSearchResults() {
        searchResults.classList.add('show');
    }
    
    /**
     * Hide search results
     */
    function hideSearchResults() {
        searchResults.classList.remove('show');
    }
});
