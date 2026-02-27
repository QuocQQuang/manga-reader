/**
 * Real-time search functionality for manga reader
 * Handles search bar autocomplete results
 */

$(document).ready(function() {
    // Variables for search delay
    let searchTimer;
    const searchDelay = 300; // milliseconds
    
    // Initialize search functionality
    initializeSearch();
    
    function initializeSearch() {
        const $searchInput = $('#headerSearchInput');
        
        // Add event listener for input changes
        $searchInput.on('input', function() {
            const query = $(this).val();
            clearTimeout(searchTimer);
            
            // Hide results if query is too short
            if (query.length < 2) {
                $('#headerSearchResults').hide();
                return;
            }
            
            // Debounce the search to avoid too many requests
            searchTimer = setTimeout(function() {
                fetchSearchResults(query);
            }, searchDelay);
        });
        
        // Hide search results when clicking outside
        $(document).on('click', function(e) {
            if (!$(e.target).closest('#headerSearchInput, #headerSearchResults').length) {
                $('#headerSearchResults').hide();
            }
        });
    }
    
    // Fetch search results from server
    function fetchSearchResults(query) {
        $.ajax({
            url: '/search/autocomplete',
            data: { term: query },
            success: function(data) {
                displaySearchResults(data);
            },
            error: function() {
                console.error('Failed to fetch search results');
            }
        });
    }
    
    // Display search results in dropdown
    function displaySearchResults(results) {
        const $resultsContainer = $('#headerSearchResults');
        $resultsContainer.empty();
        
        if (results.length === 0) {
            $resultsContainer.hide();
            return;
        }
        
        // Create and append result items
        results.forEach(function(item) {
            const $resultItem = $('<div class="search-result-item"></div>')
                .text(item.value)
                .on('click', function() {
                    window.location.href = '/manga/' + item.id;
                });
                
            $resultsContainer.append($resultItem);
        });
        
        // Show results container
        $resultsContainer.show();
    }
});