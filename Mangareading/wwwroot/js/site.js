/**
 * Main JavaScript Entry Point
 * Loads and initializes components for the Manga Reader application
 */

// Component loader
class ComponentLoader {
    /**
     * Initialize the component loader
     */
    constructor() {
        // Define components to load based on page type
        this.components = {
            // Common components for all pages
            common: [
                '/js/utils/dom-helpers.js',
                '/js/utils/date-formatter.js',
                '/js/components/theme-switcher.js',
                '/js/components/searchbar.js'
            ],

            // Layout-specific components
            layouts: {
                main: ['/js/layouts/main-layout.js'],
                reading: ['/js/layouts/reading-layout.js'],
                admin: ['/js/layouts/admin-layout.js']
            }
        };

        // Initialize
        this.init();
    }

    /**
     * Initialize the component loader
     */
    init() {
        // Load common components
        this.loadScripts(this.components.common);

        // Detect layout type and load appropriate components
        this.detectLayoutAndLoadComponents();

        // Initialize chapter filter if present
        this.initChapterFilter();
        
        // Initialize manga list pagination
        this.initMangaListPagination();
    }

    /**
     * Detect layout type and load appropriate components
     */
    detectLayoutAndLoadComponents() {
        if (document.body.classList.contains('reading-mode')) {
            // Reading layout
            this.loadScripts(this.components.layouts.reading);
        } else if (document.body.classList.contains('admin-panel')) {
            // Admin layout
            this.loadScripts(this.components.layouts.admin);
        } else {
            // Default to main layout
            this.loadScripts(this.components.layouts.main);
        }
    }

    /**
     * Load scripts dynamically
     * param {Array} scripts - Array of script URLs to load
     */
    loadScripts(scripts) {
        scripts.forEach(src => {
            // Skip if script is already loaded
            if (document.querySelector(`script[src="${src}"]`)) {
                return;
            }

            // Create script element
            const script = document.createElement('script');
            script.src = src;
            script.async = true;

            // Append to document
            document.body.appendChild(script);
        });
    }

    /**
     * Initialize chapter filter functionality
     */
    initChapterFilter() {
        const chapterFilter = document.getElementById('chapterFilter');

        if (chapterFilter) {
            chapterFilter.addEventListener('input', function() {
                const filter = this.value.toLowerCase();
                const chapterItems = document.querySelectorAll('.chapter-item');

                chapterItems.forEach(item => {
                    const text = item.textContent.toLowerCase();
                    item.style.display = text.includes(filter) ? '' : 'none';
                });

                // Show/hide no results message
                const noResultsMessage = document.getElementById('noChapterResults');
                if (noResultsMessage) {
                    const visibleItems = document.querySelectorAll('.chapter-item[style=""]').length;
                    noResultsMessage.style.display = visibleItems === 0 ? 'block' : 'none';
                }
            });
        }
    }
    
    /**
     * Initialize manga list pagination
     */
    initMangaListPagination() {
        // Handle page navigation for manga list
        const paginationContainer = document.querySelector('.pagination-container');
        if (paginationContainer) {
            // Handle pagination click events
            paginationContainer.addEventListener('click', function(e) {
                // Find closest page link if clicked on a child element
                const pageLink = e.target.closest('.page-link');
                
                if (pageLink && !pageLink.closest('.disabled')) {
                    e.preventDefault();
                    
                    // Get the page URL from the link
                    const pageUrl = pageLink.getAttribute('href');
                    if (pageUrl) {
                        // Navigate to the page
                        window.location.href = pageUrl;
                    }
                }
            });
            
            // Handle direct page input form
            const pageInputForm = document.querySelector('.pagination-goto form');
            if (pageInputForm) {
                pageInputForm.addEventListener('submit', function(e) {
                    e.preventDefault();
                    
                    const pageInput = this.querySelector('input[name="page"]');
                    if (pageInput) {
                        const page = parseInt(pageInput.value);
                        const min = parseInt(pageInput.getAttribute('min') || 1);
                        const max = parseInt(pageInput.getAttribute('max') || 1000);
                        
                        // Validate page number
                        if (page >= min && page <= max) {
                            // Build the URL with the current parameters and new page
                            const currentUrl = new URL(window.location.href);
                            const params = new URLSearchParams(currentUrl.search);
                            params.set('page', page);
                            
                            // Navigate to the new URL
                            window.location.href = `${currentUrl.pathname}?${params.toString()}`;
                        } else {
                            // Show error message
                            alert(`Please enter a page number between ${min} and ${max}.`);
                            pageInput.value = Math.max(min, Math.min(max, page));
                        }
                    }
                });
            }
        }
    }
}

// Initialize component loader when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    new ComponentLoader();
});

// Rankings functionality
$(document).ready(function() {
    // Initial loading
    const defaultPeriod = 'week';
    loadRankings('views', defaultPeriod);
    
    // Tab switching
    $('#rankingTabs .nav-link').on('click', function() {
        const rankingType = $(this).attr('id').split('-')[0];
        const activePeriod = $('.period-btn.active').data('period');
        loadRankings(rankingType, activePeriod);
    });
    
    // Period switching
    $('.period-btn').on('click', function() {
        $('.period-btn').removeClass('active');
        $(this).addClass('active');
        
        const period = $(this).data('period');
        const activeTab = $('#rankingTabs .nav-link.active').attr('id').split('-')[0];
        loadRankings(activeTab, period);
    });
    
    function loadRankings(type, period) {
        // Show loader, hide content
        $(`#${type}RankingLoader`).removeClass('d-none');
        $(`#${type}RankingContent`).addClass('d-none');
        
        // Determine the correct endpoint based on the type
        const endpoint = type === 'views' ? '/api/Stats/top-manga' : '/api/Stats/top-favorites';
        
        $.ajax({
            url: endpoint,
            data: { period: period, count: 10 }, // Use count parameter instead of pagination
            method: 'GET',
            success: function(response) {
                renderRankings(type, response.manga);
            },
            error: function() {
                showRankingError(type);
            }
        });
    }
    
    function renderRankings(type, items) {
        // Hide loader
        $(`#${type}RankingLoader`).addClass('d-none');
        
        // Clear previous items
        const $rankingList = $(`#${type}RankingContent .ranking-list`);
        $rankingList.empty();
        
        if (!items || items.length === 0) {
            $rankingList.append(`
                <li class="list-group-item text-center py-4">
                    <p class="text-muted mb-0">No rankings available for this period.</p>
                </li>
            `);
        } else {
            // Add new items
            items.forEach((item, index) => {
                let positionClass = '';
                if (index === 0) positionClass = 'position-first';
                else if (index === 1) positionClass = 'position-second';
                else if (index === 2) positionClass = 'position-third';
                
                let statusBadge = '';
                if (item.status === 'ongoing') {
                    statusBadge = '<span class="badge bg-success">Ongoing</span>';
                } else if (item.status === 'completed') {
                    statusBadge = '<span class="badge bg-warning">Completed</span>';
                } else if (item.status === 'hiatus') {
                    statusBadge = '<span class="badge bg-danger">Hiatus</span>';
                }
                
                $rankingList.append(`
                    <li class="list-group-item ranking-item d-flex align-items-center p-2">
                        <div class="position-badge ${positionClass} me-3">${index + 1}</div>
                        <a href="/manga/${item.mangaId}" class="d-flex align-items-center flex-grow-1 text-decoration-none">
                            <div class="ranking-cover me-3">
                                <img src="${item.coverUrl || '/images/no-cover.png'}" alt="${item.title}" class="img-fluid rounded" width="60" onerror="this.src='/images/no-cover.png';">
                            </div>
                            <div class="ranking-info">
                                <h6 class="ranking-title mb-1">${item.title}</h6>
                                <div class="d-flex flex-wrap">
                                    ${statusBadge}
                                    <small class="text-muted ms-2">
                                        <i class="fas fa-eye me-1"></i> ${item.viewCount?.toLocaleString() || 0}
                                    </small>
                                    <small class="text-muted ms-2">
                                        <i class="fas fa-heart me-1"></i> ${item.favoriteCount?.toLocaleString() || 0}
                                    </small>
                                </div>
                            </div>
                        </a>
                    </li>
                `);
            });
        }
        
        // Show content
        $(`#${type}RankingContent`).removeClass('d-none');
    }
    
    function showRankingError(type) {
        // Hide loader
        $(`#${type}RankingLoader`).addClass('d-none');
        
        // Show error message
        const $rankingList = $(`#${type}RankingContent .ranking-list`);
        $rankingList.empty();
        $rankingList.append(`
            <li class="list-group-item text-center py-4">
                <div class="text-danger mb-2">
                    <i class="fas fa-exclamation-circle fa-2x"></i>
                </div>
                <p class="text-muted mb-0">Failed to load rankings. Please try again later.</p>
            </li>
        `);
        
        // Show content
        $(`#${type}RankingContent`).removeClass('d-none');
    }
});