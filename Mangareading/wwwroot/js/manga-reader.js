/**
 * Manga Reader JavaScript (v2.0 - Merged)
 * Handles manga reading functionality including page navigation, view tracking, UI controls, settings, and progress saving.
 * Merged from manga-reader.js and reading.js
 */

// Create a global MangaReader object
window.MangaReader = (function($) {
    // Private variables
    let mangaId;
    let chapterId;
    let isAuthenticated;
    let currentPage = 1;
    let totalPages;
    let readingMode; // 'vertical', 'horizontal' (from settings panel)
    let pageFit; // 'width', 'height', 'original' (from settings panel)
    let bgColor; // 'dark', 'black', 'gray', 'light' (from settings panel)

    let lastScrollTop = 0;
    let isNavVisible = true;
    let isSettingsVisible = false;
    let isHotkeyInfoVisible = false;
    let header;
    let footer;
    let settingsPanel;
    let hotkeyInfoPanel;
    let saveProgressTimeout;
    let isScrolling = false;
    let scrollingElement;
    let currentScrollPosition;
    let targetScrollPosition;
    let scrollAnimationId = null;
    let horizontalNavigationEnabled = false;

    /**
     * Initialize the manga reader
     */
    function initializeReader() {
        // Get initial data from DOM
        mangaId = $('#mangaId').val();
        chapterId = $('#chapterId').val();
        isAuthenticated = $('#isLoggedIn').val() === 'true';
        totalPages = $('.manga-page').length;
        header = $('#readingHeader'); // Use ID from reading.js
        footer = $('#readingFooter'); // Use ID from reading.js
        settingsPanel = $('#readingSettings'); // Use ID from _ReadingLayout.cshtml
        hotkeyInfoPanel = $('#hotkeyInfo'); // Use ID from _ReadingLayout.cshtml
        scrollingElement = document.scrollingElement || document.documentElement;
        currentScrollPosition = scrollingElement.scrollTop;
        targetScrollPosition = currentScrollPosition;


        // Load settings first
        loadUserSettings(); // From reading.js

        // Initialize components
        initializeLazyLoading(); // From manga-reader.js (enhanced)
        initializeScrollHandler(); // From reading.js
        initializeSettingsPanel(); // Combined logic
        initializeKeyboardShortcuts(); // Combined logic
        initializeScrollButtons(); // From manga-reader.js
        initializeSmoothScroll(); // From reading.js
        initializeHorizontalNavigation(); // New function for horizontal navigation

        // Initial UI updates
        updateReadingProgressDisplay(); // Update display based on initial state or loaded progress

        // Record view
        recordView(mangaId, chapterId); // From manga-reader.js

        // Record view on chapter navigation click
        $('.prev-chapter-link, .next-chapter-link').click(function(e) {
            const href = $(this).attr('href');
            // Extract chapterId robustly, handling potential trailing slashes or query params
            const pathSegments = href.split('/').filter(segment => segment);
            const nextChapterId = parseInt(pathSegments[pathSegments.length - 1]);
            if (!isNaN(nextChapterId)) {
                recordView(mangaId, nextChapterId);
            }
        });

        // Initialize network monitor (if needed, assuming MangaReader object provides it)
        // initNetworkMonitor(); // Placeholder if this function exists within MangaReader

        console.log('Manga Reader v2.0 Initialized:', {
            mangaId: mangaId,
            chapterId: chapterId,
            totalPages: totalPages,
            isAuthenticated: isAuthenticated,
            settings: { readingMode, pageFit, bgColor }
        });

        // Removed welcome message (2025-04-22)
        // setTimeout(() => {
        //     const currentUser = document.querySelector('.user-badge span')?.textContent || 'Guest';
        //     if (typeof showToast === 'function') {
        //         showToast(`Welcome ${currentUser}! Enjoy reading!`, 'info', 4000);
        //     } else {
        //         console.log(`Welcome ${currentUser}! Enjoy reading!`);
        //     }
        // }, 2000);
    }

    /**
     * Initialize horizontal page navigation controls
     * (NEW: Add support for clicking left/right areas to navigate in horizontal mode)
     */
    function initializeHorizontalNavigation() {
        // Click event handlers for left/right navigation controls
        $('#prevPageControl').on('click', function() {
            if (readingMode !== 'horizontal') return;
            navigateToAdjacentPage('prev');
        });

        $('#nextPageControl').on('click', function() {
            if (readingMode !== 'horizontal') return;
            navigateToAdjacentPage('next');
        });

        // Add mousewheel support for horizontal reading
        $('.manga-pages-container').on('wheel', function(e) {
            if (readingMode !== 'horizontal') return;
            
            e.preventDefault();
            const delta = e.originalEvent.deltaY;
            
            if (delta > 0) {
                // Scroll down/right
                navigateToAdjacentPage('next');
            } else {
                // Scroll up/left
                navigateToAdjacentPage('prev');
            }
        });

        // Add keyboard left/right arrows for horizontal reading
        $(document).on('keydown', function(e) {
            if (readingMode !== 'horizontal') return;
            if ($(e.target).is('input, textarea') || e.metaKey || e.ctrlKey || e.altKey) return;

            switch (e.key) {
                case 'ArrowLeft':
                    // Don't navigate to previous chapter, but to previous page
                    if ($('.manga-pages-container').is(':hover')) {
                        e.preventDefault();
                        navigateToAdjacentPage('prev');
                    }
                    break;
                case 'ArrowRight':
                    // Don't navigate to next chapter, but to next page
                    if ($('.manga-pages-container').is(':hover')) {
                        e.preventDefault();
                        navigateToAdjacentPage('next');
                    }
                    break;
            }
        });

        // Set a flag to indicate that horizontal navigation is enabled
        horizontalNavigationEnabled = true;
        console.log('Horizontal navigation controls initialized');
    }

    /**
     * Navigate to the previous or next page in horizontal reading mode
     * @param {string} direction - 'prev' or 'next'
     */
    function navigateToAdjacentPage(direction) {
        if (readingMode !== 'horizontal') return;

        const container = $('.manga-pages-container')[0];
        if (!container) return;
        
        // Get all page containers
        const pages = $('.page-container');
        if (!pages.length) return;

        // Find the current page container by checking which one is most visible
        let currentPageContainer = null;
        let maxVisibleArea = 0;
        
        // Calculate container's visible area
        const containerRect = container.getBoundingClientRect();
        const containerLeft = containerRect.left;
        const containerRight = containerRect.right;
        const containerWidth = containerRect.width;

        pages.each(function() {
            const pageRect = this.getBoundingClientRect();
            
            // Calculate the intersection of the page with the viewport
            const visibleLeft = Math.max(pageRect.left, containerLeft);
            const visibleRight = Math.min(pageRect.right, containerRight);
            
            if (visibleLeft < visibleRight) {
                const visibleArea = visibleRight - visibleLeft;
                if (visibleArea > maxVisibleArea) {
                    maxVisibleArea = visibleArea;
                    currentPageContainer = this;
                }
            }
        });

        // If no page is visible (shouldn't happen), use the first page
        if (!currentPageContainer) {
            currentPageContainer = pages[0];
        }

        // Find the target page
        let targetPage = null;
        if (direction === 'prev') {
            targetPage = $(currentPageContainer).prev('.page-container')[0];
            if (!targetPage && prevChapterAvailable()) {
                // Go to previous chapter if available
                navigateToPrevChapter();
                return;
            }
        } else { // next
            targetPage = $(currentPageContainer).next('.page-container')[0];
            if (!targetPage && nextChapterAvailable()) {
                // Go to next chapter if available
                navigateToNextChapter();
                return;
            }
        }

        // If we have a target page, scroll to it
        if (targetPage) {
            scrollToPageInHorizontalMode(targetPage);
            
            // Update current page and save reading progress
            const pageNumber = parseInt($(targetPage).find('.manga-page').data('page'));
            if (!isNaN(pageNumber) && pageNumber !== currentPage) {
                currentPage = pageNumber;
                updateReadingProgressDisplay();
                saveReadingProgress(currentPage);
            }
        }
    }

    /**
     * Check if a previous chapter is available
     */
    function prevChapterAvailable() {
        return $('.prev-chapter-link').length > 0;
    }

    /**
     * Check if a next chapter is available
     */
    function nextChapterAvailable() {
        return $('.next-chapter-link').length > 0;
    }

    /**
     * Navigate to the previous chapter
     */
    function navigateToPrevChapter() {
        const prevChapterLink = $('.prev-chapter-link').attr('href');
        if (prevChapterLink) {
            window.location.href = prevChapterLink;
        }
    }

    /**
     * Navigate to the next chapter
     */
    function navigateToNextChapter() {
        const nextChapterLink = $('.next-chapter-link').attr('href');
        if (nextChapterLink) {
            window.location.href = nextChapterLink;
        }
    }

    /**
     * Scroll to a specific page in horizontal reading mode
     * @param {HTMLElement} pageElement - The page element to scroll to
     */
    function scrollToPageInHorizontalMode(pageElement) {
        if (!pageElement) return;
        
        const container = $('.manga-pages-container')[0];
        if (!container) return;
        
        // Use smooth scrolling to scroll the page into view
        pageElement.scrollIntoView({
            behavior: 'smooth',
            block: 'center',
            inline: 'center'
        });
    }

    /**
     * Initialize scroll handler to hide/show header/footer
     * (From reading.js)
     */
    function initializeScrollHandler() {
        $(window).scroll(function () {
            const st = $(this).scrollTop();

            // Ignore small scrolls
            if (Math.abs(lastScrollTop - st) < 15) return; // Increased threshold

            // Scrolling down
            if (st > lastScrollTop && st > 100) {
                if (isNavVisible) {
                    header.addClass('hidden');
                    footer.addClass('hidden');
                    isNavVisible = false;
                }
            }
            // Scrolling up or near top
            else if (st < lastScrollTop || st <= 100) {
                 if (!isNavVisible) {
                    header.removeClass('hidden');
                    footer.removeClass('hidden');
                    isNavVisible = true;
                }
            }

            lastScrollTop = st;

            // Update current page based on scroll
            updateCurrentPage(); // From manga-reader.js logic
        });
    }


    /**
     * Initialize lazy loading for manga pages
     * (Enhanced version from manga-reader.js)
     */
    function initializeLazyLoading() {
        // Load first few images immediately
        loadInitialImages();

        // Set up intersection observer for remaining images
        const options = {
            root: null, // viewport
            rootMargin: '300px 0px', // Load images 300px below/above viewport
            threshold: 0.01 // Trigger even if 1% is visible
        };

        const observer = new IntersectionObserver((entries, obs) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    // **Add check here:** Ensure the element is an image and has necessary data attributes
                    const src = img.dataset.src || img.getAttribute('data-src');
                    const pageNum = img.dataset.page || img.getAttribute('data-page');

                    if (img.tagName === 'IMG' && src && pageNum) {
                        // Check if it's still marked as loading
                        if (img.classList.contains('loading')) {
                            loadImage(img); // Call loadImage only if checks pass
                        }
                        obs.unobserve(img); // Unobserve after triggering load or confirming it was already loaded/handled
                    } else {
                        // Log a warning if the element is intersecting but lacks necessary data
                        console.warn('Intersection observed for element without valid data-src/data-page:', img);
                        // Optionally unobserve here too to prevent repeated warnings for the same invalid element
                        obs.unobserve(img);
                    }
                }
            });
        }, options);

        // Observe all images marked with 'loading' class
        document.querySelectorAll('.manga-page.loading').forEach(img => {
            observer.observe(img);
        });

         // Add animation delay for nice fade-in effect (from reading.js)
        $('.page-container').each(function (index) {
            $(this).css('animation-delay', (index * 0.05) + 's'); // Faster animation
        });
    }

    /**
     * Load the first few images immediately
     * (From manga-reader.js)
     */
    function loadInitialImages() {
        const initialImagesToLoad = 3; // Load first 3 images
        document.querySelectorAll('.manga-page.loading').forEach((img, index) => {
            if (index < initialImagesToLoad) {
                loadImage(img); // Use the advanced loader
            }
        });
    }

    /**
     * Get image dimensions before loading (cached in sessionStorage)
     * (From manga-reader.js)
     */
    function getImageDimensions(src) {
        return new Promise((resolve, reject) => {
            // Check cache first
            const cacheKey = `img_dim_${src}`;
            const cachedDimensions = sessionStorage.getItem(cacheKey);
            if (cachedDimensions) {
                try {
                    resolve(JSON.parse(cachedDimensions));
                    return;
                } catch (e) {
                    console.warn('Error parsing cached dimensions, fetching again:', e);
                    sessionStorage.removeItem(cacheKey); // Remove invalid cache entry
                }
            }

            // Create a temporary image to measure
            const tempImg = new Image();
            let timeoutId = null;

            tempImg.onload = function() {
                clearTimeout(timeoutId); // Clear timeout on successful load
                const dimensions = {
                    width: this.naturalWidth, // Use naturalWidth/Height
                    height: this.naturalHeight,
                    ratio: this.naturalHeight / this.naturalWidth
                };

                // Cache the dimensions
                try {
                    sessionStorage.setItem(cacheKey, JSON.stringify(dimensions));
                } catch (e) {
                    console.error('Error caching image dimensions:', e);
                }
                resolve(dimensions);
            };

            tempImg.onerror = function() {
                clearTimeout(timeoutId); // Clear timeout on error
                console.error('Failed to load image for dimensions:', src);
                reject(new Error('Failed to load image for dimensions'));
            };

            // Set a timeout for dimension fetching (e.g., 10 seconds)
            timeoutId = setTimeout(() => {
                tempImg.onload = null; // Prevent late onload call
                tempImg.onerror = null; // Prevent late onerror call
                console.error('Timeout fetching image dimensions:', src);
                reject(new Error('Timeout fetching image dimensions'));
            }, 10000);

            // Start loading the temporary image
            tempImg.src = src;
        });
    }

    /**
     * Create placeholder with exact dimensions or default
     * (Enhanced from manga-reader.js)
     */
    function createPlaceholder(img, dimensions) {
        const container = img.closest('.page-container');
        if (!container) return null; // Should not happen

        // Remove any existing placeholders first
        container.querySelectorAll('.image-placeholder').forEach(el => el.remove());

        // Create new placeholder
        const placeholder = document.createElement('div');
        placeholder.className = 'image-placeholder loading'; // Base class + loading state
        // Prepend placeholder so image loads over it visually if needed
        container.prepend(placeholder);

        let placeholderContent = `
            <div class="placeholder-content">
                <div class="spinner-border text-secondary" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <div class="placeholder-info mt-2 text-muted small">
                    <span>Loading image...</span>
                </div>
            </div>`;

        // Set dimensions and potentially more detailed content
        if (dimensions && dimensions.width > 0 && dimensions.height > 0) {
            const containerWidth = container.clientWidth || container.parentNode.clientWidth; // Get width reliably
            // Calculate height based on container width and image aspect ratio
            const calculatedHeight = containerWidth * dimensions.ratio;

            placeholder.style.width = '100%'; // Always full width of container
            placeholder.style.height = `${calculatedHeight}px`;
            // Use aspect-ratio CSS property for modern browsers
            placeholder.style.aspectRatio = `${dimensions.width} / ${dimensions.height}`;

            placeholderContent = `
                <div class="placeholder-content">
                    <div class="spinner-border text-secondary" role="status">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                    <div class="placeholder-info mt-2 text-muted small">
                        <span class="placeholder-dimensions">${dimensions.width} × ${dimensions.height}</span>
                    </div>
                </div>`;
        } else {
            // Default placeholder if dimensions are unknown or invalid
            placeholder.style.width = '100%';
            // Estimate height based on common aspect ratio (e.g., 1.5) or a fixed height
            const containerWidth = container.clientWidth || container.parentNode.clientWidth;
            placeholder.style.height = `${Math.min(containerWidth * 1.5, window.innerHeight * 0.8)}px`; // Estimate height
        }

        placeholder.innerHTML = placeholderContent;
        return placeholder;
    }

    /**
     * Create an error placeholder with retry functionality
     * (Enhanced from manga-reader.js and reading.js)
     */
    function createErrorPlaceholder(img, message, src, pageNum) {
        const container = img.closest('.page-container');
        if (!container) return null;

        // Remove existing placeholders/image
        container.querySelectorAll('.image-placeholder, .manga-page').forEach(el => {
            if (el !== img) el.remove(); // Remove old placeholders
        });
        img.style.display = 'none'; // Hide the broken image element

        // Create new placeholder
        const placeholder = document.createElement('div');
        placeholder.className = 'image-placeholder error-placeholder'; // Base class + error state
        container.prepend(placeholder); // Prepend

        // Try to get stored dimensions for better placeholder sizing
        let width = '100%';
        let height = '300px'; // Default height
        let aspectRatioStyle = '';

        try {
            const cachedDimensions = sessionStorage.getItem(`img_dim_${src}`);
            if (cachedDimensions) {
                const dimensions = JSON.parse(cachedDimensions);
                if (dimensions && dimensions.width > 0 && dimensions.height > 0) {
                    const containerWidth = container.clientWidth || container.parentNode.clientWidth;
                    const calculatedHeight = containerWidth * dimensions.ratio;
                    height = `${calculatedHeight}px`;
                    aspectRatioStyle = `aspect-ratio: ${dimensions.width} / ${dimensions.height};`;
                }
            } else {
                 // Estimate height if no dimensions cached
                 const containerWidth = container.clientWidth || container.parentNode.clientWidth;
                 height = `${Math.min(containerWidth * 1.5, window.innerHeight * 0.8)}px`;
            }
        } catch (e) {
            console.warn('Error using cached dimensions for error placeholder:', e);
             const containerWidth = container.clientWidth || container.parentNode.clientWidth;
             height = `${Math.min(containerWidth * 1.5, window.innerHeight * 0.8)}px`;
        }

        // Set placeholder dimensions
        placeholder.style.width = width;
        placeholder.style.height = height;
        if (aspectRatioStyle) placeholder.style.cssText += aspectRatioStyle;

        // Add error message, retry button, and details
        placeholder.innerHTML = `
            <div class="placeholder-content">
                <div class="placeholder-icon">
                    <i class="fas fa-exclamation-triangle text-danger fa-2x"></i>
                </div>
                <div class="placeholder-message mt-2">
                    ${message || `Page ${pageNum} failed to load.`}
                </div>
                <button class="retry-button btn btn-warning btn-sm mt-3" data-src="${src}" data-page="${pageNum}">
                    <i class="fas fa-sync-alt me-1"></i> Retry
                </button>
                <div class="error-detail small text-muted mt-2">
                    <a href="#" class="toggle-error-detail">Show Details</a>
                    <div class="error-detail-content d-none mt-1" style="word-break: break-all;">
                        <div>URL: ${src}</div>
                        <div>Page: ${pageNum}</div>
                        <div>Time: ${new Date().toLocaleTimeString()}</div>
                    </div>
                </div>
            </div>
        `;

        // Add manual retry handler (using event delegation might be better if many errors occur)
        const retryButton = placeholder.querySelector('.retry-button');
        if (retryButton) {
            retryButton.addEventListener('click', function() {
                const button = this;
                const imgSrc = button.getAttribute('data-src');
                button.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i> Retrying...';
                button.disabled = true;

                // Find the original image element (it's hidden)
                const originalImg = container.querySelector('.manga-page');

                // Try to load the image again
                const retryImg = new Image();
                retryImg.onload = function() {
                    // Success! Replace placeholder with the original image element, update its src
                    if (originalImg) {
                        originalImg.src = imgSrc; // Set src again
                        originalImg.style.display = ''; // Show it
                        originalImg.classList.remove('error', 'loading');
                        originalImg.classList.add('loaded');
                        placeholder.remove(); // Remove error placeholder
                    } else {
                         // Fallback if original img element is lost (shouldn't happen)
                         const newImg = document.createElement('img');
                         newImg.className = 'manga-page loaded';
                         newImg.src = imgSrc;
                         newImg.dataset.page = pageNum;
                         container.prepend(newImg);
                         placeholder.remove();
                    }
                };
                retryImg.onerror = function() {
                    // Still failing - reset button
                    button.innerHTML = '<i class="fas fa-sync-alt me-1"></i> Retry Failed';
                    button.disabled = false;
                    // Optionally add a delay before enabling retry again
                    setTimeout(() => {
                         button.innerHTML = '<i class="fas fa-sync-alt me-1"></i> Retry';
                    }, 2000);
                };
                retryImg.src = imgSrc; // Start loading
            });
        }

        // Add toggle for error details
        const toggleLink = placeholder.querySelector('.toggle-error-detail');
        if (toggleLink) {
            toggleLink.addEventListener('click', function(e) {
                e.preventDefault();
                const detailContent = this.closest('.error-detail').querySelector('.error-detail-content');
                const isHidden = detailContent.classList.contains('d-none');
                detailContent.classList.toggle('d-none', !isHidden);
                this.textContent = isHidden ? 'Hide Details' : 'Show Details';
            });
        }

        console.error(`Image load failed for page ${pageNum}: ${src}`);
        return placeholder;
    }


    /**
     * Load a single image with placeholder and error handling
     * (Enhanced from manga-reader.js)
     */
    function loadImage(img) {
        const src = img.dataset.src; // Use dataset.src
        const pageNum = img.dataset.page;

        // Basic check for invalid src
        if (!src || src === 'null' || src.includes('/null')) {
            console.error(`Invalid image source for page ${pageNum}: ${src}`);
            img.classList.remove('loading');
            createErrorPlaceholder(img, `Invalid image source`, src || 'N/A', pageNum);
            return;
        }

        // Ensure image is initially hidden while we work
        img.style.display = 'none';
        img.classList.add('loading'); // Ensure loading class is present

        let currentPlaceholder = createPlaceholder(img); // Create default placeholder

        // Try to get dimensions first to create a better placeholder
        getImageDimensions(src)
            .then(dimensions => {
                // Update placeholder with actual dimensions
                currentPlaceholder = createPlaceholder(img, dimensions);

                // Now attempt to load the actual image
                loadAndAttachImage(img, src, pageNum, currentPlaceholder);
            })
            .catch(() => {
                // If getting dimensions fails, still try to load the image directly
                console.warn(`Could not get dimensions for ${src}, loading directly.`);
                loadAndAttachImage(img, src, pageNum, currentPlaceholder);
            });
    }

    /** Helper function to load the image and handle onload/onerror */
    function loadAndAttachImage(imgElement, src, pageNum, placeholderElement) {
        let loadTimeoutId = null;

        imgElement.onload = function() {
            clearTimeout(loadTimeoutId);
            // Success: Show the image, remove loading state, remove placeholder
            imgElement.style.display = '';
            imgElement.classList.remove('loading');
            imgElement.classList.add('loaded');

            if (placeholderElement && placeholderElement.parentNode) {
                placeholderElement.classList.add('fade-out');
                setTimeout(() => {
                    if (placeholderElement.parentNode) {
                        placeholderElement.remove();
                    }
                }, 300); // Match fade-out duration
            }
            // Reset error handler to prevent potential issues if src is changed later
            imgElement.onerror = null;
        };

        imgElement.onerror = function() {
            clearTimeout(loadTimeoutId);
            // Error: Remove loading state, create error placeholder
            imgElement.classList.remove('loading');
            imgElement.classList.add('error');
            if (placeholderElement && placeholderElement.parentNode) {
                placeholderElement.remove(); // Remove loading placeholder
            }
            createErrorPlaceholder(imgElement, `Page ${pageNum} failed to load.`, src, pageNum);
             // Reset onload handler
            imgElement.onload = null;
        };

        // Set a timeout for image loading (e.g., 15 seconds)
        loadTimeoutId = setTimeout(() => {
            if (!imgElement.complete || imgElement.naturalWidth === 0) { // Check if not loaded
                 console.error(`Timeout loading image for page ${pageNum}: ${src}`);
                 imgElement.onerror(); // Trigger the error handler
            }
        }, 15000); // 15 second timeout

        // Start loading the image
        imgElement.src = src;
    }


    /**
     * Update the current page based on scroll position
     * (Logic from manga-reader.js - finding closest page to center)
     */
    function updateCurrentPage() {
        const windowHeight = $(window).height();
        // Use a point slightly above center for better feel when scrolling down
        const scrollCheckPoint = $(window).scrollTop() + (windowHeight * 0.4);

        let closestPageNum = 1;
        let minDistance = Infinity;
        let foundPageInView = false;

        $('.manga-page').each(function() {
            const page = $(this);
            // Ensure offset() is valid before proceeding
             if (!page.offset()) return;

            const pageTop = page.offset().top;
            const pageHeight = page.height();
            // Check if the scrollCheckPoint is within this page's bounds
            if (scrollCheckPoint >= pageTop && scrollCheckPoint < pageTop + pageHeight) {
                closestPageNum = parseInt(page.data('page'));
                foundPageInView = true;
                return false; // Exit loop once page in view is found
            }

            // If no page contains the checkpoint yet, find the closest one
            if (!foundPageInView) {
                 const pageMiddle = pageTop + (pageHeight / 2);
                 const distance = Math.abs(scrollCheckPoint - pageMiddle);
                 if (distance < minDistance) {
                     minDistance = distance;
                     closestPageNum = parseInt(page.data('page'));
                 }
            }
        });

        // Update if page changed
        if (currentPage !== closestPageNum) {
            currentPage = closestPageNum;
            updateReadingProgressDisplay(); // Update UI display
            saveReadingProgress(currentPage); // Save progress (throttled)
        }
    }

    /**
     * Update the reading progress UI display
     * (Combined from both files)
     */
    function updateReadingProgressDisplay() {
        if (totalPages > 0) {
            const progressPercent = Math.max(0, Math.min(100, Math.round((currentPage / totalPages) * 100)));
            $('#currentPageNumber').text('Page ' + currentPage); // Use 'Page' for consistency
            $('#readingProgressValue').text(progressPercent + '%');
        } else {
             $('#currentPageNumber').text('Page N/A');
             $('#readingProgressValue').text('0%');
        }
    }

    /**
     * Save reading progress to localStorage and server (throttled)
     * (From reading.js, uses MangaTracking if available)
     */
    function saveReadingProgress(pageNumber) {
        if (!chapterId) return; // Don't save if chapterId is missing

        localStorage.setItem(`reading_${chapterId}`, pageNumber);
        updateSyncStatusIndicator('saving'); // Indicate saving locally

        if (isAuthenticated) {
            // Throttle server updates
            if (saveProgressTimeout) {
                clearTimeout(saveProgressTimeout);
            }

            saveProgressTimeout = setTimeout(() => {
                const mangaIdVal = mangaId; // Ensure mangaId is available
                const chapterIdVal = chapterId;
                const pageNumVal = pageNumber;
                const totalPagesVal = totalPages;

                if (!mangaIdVal || !chapterIdVal || totalPagesVal <= 0) {
                     console.warn("Cannot save progress to server, missing data:", { mangaIdVal, chapterIdVal, totalPagesVal });
                     updateSyncStatusIndicator('local'); // Indicate local save only
                     return;
                }

                // Try to use MangaTracking module first
                if (typeof MangaTracking !== 'undefined' && MangaTracking.updateReadingProgress) {
                    console.log('Saving progress via MangaTracking module...');
                    MangaTracking.updateReadingProgress(
                        mangaIdVal,
                        chapterIdVal,
                        pageNumVal,
                        totalPagesVal
                    );

                    // Check sync status after a short delay (allow MangaTracking to attempt sync)
                    setTimeout(() => {
                        // MangaTracking module might set this if sync fails repeatedly
                        if (localStorage.getItem('server_progress_unavailable') === 'true') {
                            updateSyncStatusIndicator('local');
                        } else {
                            // Assume synced if no error flag is set (MangaTracking handles actual success/fail)
                            updateSyncStatusIndicator('synced');
                        }
                    }, 1500); // Wait a bit longer
                } else {
                    // Fallback to direct API call if MangaTracking is not available
                    console.log('Saving progress via fallback API call...');
                    $.ajax({
                        url: '/api/MangaTracking/progress', // Fallback endpoint from reading.js
                        method: 'POST',
                        contentType: 'application/json',
                        data: JSON.stringify({
                            mangaId: mangaIdVal,
                            chapterId: chapterIdVal,
                            currentPage: pageNumVal,
                            totalPages: totalPagesVal,
                            progressPercent: Math.round((pageNumVal / totalPagesVal) * 100)
                        }),
                        success: function() {
                            console.log('Fallback API progress save successful.');
                            updateSyncStatusIndicator('synced');
                            localStorage.removeItem('server_progress_unavailable'); // Clear error flag on success
                        },
                        error: function(xhr) {
                            console.error('Fallback API progress save failed:', xhr.responseText);
                            updateSyncStatusIndicator('local'); // Mark as local save on error
                            localStorage.setItem('server_progress_unavailable', 'true'); // Set error flag
                        }
                    });
                }
            }, 2500); // Increased throttle delay (2.5 seconds)
        } else {
            // Not logged in
            updateSyncStatusIndicator('local');
        }
    }

    /**
     * Update the sync status indicator UI
     * (From reading.js)
     */
    function updateSyncStatusIndicator(status) {
        const syncStatus = $('#syncStatus');
        if (!syncStatus.length) return; // Exit if element doesn't exist

        // Font Awesome icons
        const icons = {
            saving: 'fa-sync fa-spin', // Spinning sync icon
            synced: 'fa-cloud-check', // Cloud check icon (Font Awesome 6)
            local: 'fa-save', // Floppy disk icon
            error: 'fa-triangle-exclamation' // Warning triangle (Font Awesome 6)
        };
        const titles = {
            saving: 'Saving progress...',
            synced: 'Progress synced with server',
            local: 'Progress saved locally',
            error: 'Error saving progress to server'
        };
        const colors = { // Using text color classes
             saving: 'text-primary',
             synced: 'text-success',
             local: 'text-warning',
             error: 'text-danger'
        }

        const iconClass = icons[status] || icons.local;
        const titleText = titles[status] || titles.local;
        const colorClass = colors[status] || colors.local;

        // Update icon, title, and color
        syncStatus.html(`<i class="fas ${iconClass}"></i>`);
        syncStatus.attr('title', titleText);
        // Remove previous color classes and add the new one
        syncStatus.removeClass('text-primary text-success text-warning text-danger').addClass(colorClass);
    }


    /**
     * Load user settings from localStorage and apply them
     * (From reading.js - Aligns with _ReadingLayout.cshtml)
     */
    function loadUserSettings() {
        // Reading mode (vertical/horizontal)
        readingMode = localStorage.getItem('readingMode') || 'vertical'; // Default vertical
        $('#readingMode').val(readingMode); // Update dropdown
        applyReadingMode(readingMode);

        // Page fit (width/height/original)
        pageFit = localStorage.getItem('pageFit') || 'width'; // Default fit width
        // Populate pageFit dropdown dynamically if needed, or ensure options exist
        populatePageFitOptions(); // Helper to ensure options are present
        $('#pageFit').val(pageFit); // Update dropdown
        applyPageFit(pageFit);

        // Background color / Theme
        bgColor = localStorage.getItem('bgColor') || 'dark'; // Default dark background
        // Populate bgColor dropdown dynamically if needed, or ensure options exist
        populateBgColorOptions(); // Helper to ensure options are present
        $('#bgColor').val(bgColor); // Update dropdown
        applyBackgroundColor(bgColor); // Applies theme as well

        // Restore last reading position for this chapter
        const lastPage = localStorage.getItem(`reading_${chapterId}`);
        if (lastPage) {
            const pageNum = parseInt(lastPage);
            const pageElement = $(`[data-page="${pageNum}"]`);
            if (pageElement.length) {
                // Use smooth scroll after a short delay to allow layout to settle
                setTimeout(() => {
                    // Calculate target scroll position (e.g., top of page minus header height)
                    const headerHeight = header.outerHeight() || 60;
                    const targetScroll = pageElement.offset().top - headerHeight - 20; // Add some padding
                    smoothScrollTo(targetScroll);
                    // Update current page immediately
                    if (currentPage !== pageNum) {
                         currentPage = pageNum;
                         updateReadingProgressDisplay();
                    }
                }, 500); // Delay scrolling slightly
            }
        } else {
             // If no saved progress, update display for page 1
             updateReadingProgressDisplay();
        }
    }

     /** Helper to populate Page Fit options */
    function populatePageFitOptions() {
        const select = $('#pageFit');
        if (select.children('option').length === 0) { // Only populate if empty
            select.append($('<option>', { value: 'width', text: 'Fit Width' }));
            select.append($('<option>', { value: 'height', text: 'Fit Height' }));
            select.append($('<option>', { value: 'original', text: 'Original Size' }));
        }
    }

    /** Helper to populate Background Color options */
    function populateBgColorOptions() {
        const select = $('#bgColor');
        if (select.children('option').length === 0) { // Only populate if empty
            select.append($('<option>', { value: 'dark', text: 'Tối' })); // Changed text
            select.append($('<option>', { value: 'light', text: 'Sáng' })); // Changed text
        }
    }

    /**
     * Apply background color and corresponding theme
     * (From reading.js - Manages data-theme attribute)
     * Called when applying settings from the dropdown.
     */
    function applyBackgroundColor(color) {
        let themeValue;
        // Map background color choice to theme (light/dark)
        switch (color) {
            case 'light':
                themeValue = 'light';
                break;
            case 'dark': // Default background maps to dark theme
            default:
                themeValue = 'dark';
                color = 'dark'; // Ensure color variable is 'dark' or 'light'
                break;
        }

        console.log(`Applying background color/theme: ${themeValue}`);

        // Apply theme to HTML element using ThemeSwitcher's function for consistency
        if (window.ThemeSwitcher && typeof window.ThemeSwitcher.setTheme === 'function') {
            window.ThemeSwitcher.setTheme(themeValue, true); // Use ThemeSwitcher to set theme and trigger events
        } else {
            // Fallback if ThemeSwitcher is not available (should not happen)
            document.documentElement.setAttribute('data-theme', themeValue);
            localStorage.setItem('theme', themeValue);
            updateThemeToggleIcons(themeValue);
            console.warn("ThemeSwitcher not found, applied theme directly.");
        }

        // Update internal state and save the specific *color* choice (dark/light)
        bgColor = color;
        localStorage.setItem('bgColor', color);

        // DO NOT Trigger a custom event here, ThemeSwitcher.setTheme handles it.
    }

    /**
     * Apply reading mode (vertical/horizontal scroll)
     * (From reading.js - uses body classes)
     */
    function applyReadingMode(mode) {
        // Use body classes for global mode switching
        $('body').removeClass('reading-mode-vertical reading-mode-horizontal');

        if (mode === 'horizontal') {
            $('body').addClass('reading-mode-horizontal');
            
            // If horizontal navigation isn't already enabled, initialize it
            if (!horizontalNavigationEnabled) {
                initializeHorizontalNavigation();
            }
            
            // Make sure the first page is visible
            if ($('.page-container').length > 0) {
                setTimeout(() => {
                    scrollToPageInHorizontalMode($('.page-container')[currentPage - 1]);
                }, 200);
            }
        } else {
            $('body').addClass('reading-mode-vertical'); // Default
        }
        readingMode = mode; // Update internal state
        localStorage.setItem('readingMode', mode); // Save preference
    }

    /**
     * Apply page fit (adds classes to manga pages)
     * (Updated to target the new container structure and add body classes)
     */
    function applyPageFit(fit) {
        // Remove old classes from body and containers
        $('body').removeClass('body-fit-width body-fit-height body-fit-original'); // Add body class removal
        $('.manga-pages-container, .reading-content, .manga-page').removeClass('page-fit-width page-fit-height page-fit-original');

        // Apply to the manga-pages-container if it exists, otherwise fallback to reading-content
        const pagesContainer = $('.manga-pages-container').length > 0 ? $('.manga-pages-container') : $('.reading-content');

        switch (fit) {
            case 'height':
                $('body').addClass('body-fit-height'); // Add body class
                pagesContainer.addClass('page-fit-height');
                // Also apply directly to manga pages for legacy support (Consider removing legacy later)
                $('.manga-page').addClass('fit-height').removeClass('fit-width fit-original');
                break;
            case 'original':
                $('body').addClass('body-fit-original'); // Add body class
                pagesContainer.addClass('page-fit-original');
                // Also apply directly to manga pages for legacy support
                $('.manga-page').addClass('fit-original').removeClass('fit-width fit-height');
                break;
            case 'width': // Default
            default:
                $('body').addClass('body-fit-width'); // Add body class for width fit
                pagesContainer.addClass('page-fit-width');
                // Also apply directly to manga pages for legacy support
                $('.manga-page').addClass('fit-width').removeClass('fit-height fit-original');
                fit = 'width'; // Ensure fit is 'width' if default case hit
                break;
        }

        console.log(`Applied page fit: ${fit} to`, pagesContainer);

        // Update internal state
        pageFit = fit;
        localStorage.setItem('pageFit', fit); // Save preference
    }

    /**
     * Update theme toggle icons based on current theme
     * (From reading.js - Targets multiple buttons)
     */
    function updateThemeToggleIcons(theme) {
        // Find all theme toggle buttons by a common class or individual IDs
        const themeToggleButtons = $('.theme-toggle-button'); // Add this class to your buttons

        themeToggleButtons.each(function() {
            const icon = $(this).find('i.fas'); // Find Font Awesome icon
            if (icon.length > 0) {
                if (theme === 'dark') {
                    icon.removeClass('fa-sun').addClass('fa-moon');
                    $(this).attr('title', 'Switch to Light Mode');
                } else {
                    icon.removeClass('fa-moon').addClass('fa-sun');
                    $(this).attr('title', 'Switch to Dark Mode');
                }
            }
        });
    }


    /**
     * Initialize the settings panel interactions
     * (Combined logic, uses elements from _ReadingLayout.cshtml)
     */
    function initializeSettingsPanel() {
        // Toggle settings panel visibility
        $('#settingsToggle, #settingsToggleBottom').click(function () { // Target buttons by ID
            toggleSettings();
        });

        // Apply settings and close panel
        $('#closeSettings').click(function () {
            // Read values from selects
            const selectedMode = $('#readingMode').val();
            const selectedFit = $('#pageFit').val();
            const selectedBg = $('#bgColor').val();

            // Apply if changed
            if (selectedMode !== readingMode) applyReadingMode(selectedMode);
            if (selectedFit !== pageFit) applyPageFit(selectedFit);
            if (selectedBg !== bgColor) applyBackgroundColor(selectedBg); // This handles theme update

            // Close panel
            toggleSettings(false); // Explicitly hide
        });

        // Toggle hotkey info panel
        // Ensure you have a button with id="hotkeyToggle" if you need this
        $('#hotkeyToggle').click(function () { // Target button by ID
            toggleHotkeyInfo();
        });

        // Listen for theme changes triggered elsewhere (e.g., theme-switcher.js)
        // Use the native event dispatched by theme-switcher.js
        document.addEventListener('themeChanged', function(event) {
            const newTheme = event.detail.theme;
            console.log('MangaReader caught themeChanged event:', newTheme);

            // Map theme back to a background color for consistency in the dropdown
            // If the theme changed to dark, set dropdown to 'dark'
            // If the theme changed to light, set dropdown to 'light'
            let correspondingBgColor = (newTheme === 'light') ? 'light' : 'dark';

            // Update the bgColor dropdown selection
            if ($('#bgColor').val() !== correspondingBgColor) {
                 $('#bgColor').val(correspondingBgColor);
                 // Update internal state to match
                 bgColor = correspondingBgColor;
                 // Save the updated color choice
                 localStorage.setItem('bgColor', correspondingBgColor);
            }

            // Ensure icons are updated (ThemeSwitcher should handle this, but double-check)
            updateThemeToggleIcons(newTheme);
        });

         // REMOVED: Click handler for .theme-toggle-button to avoid conflict with theme-switcher.js
         /*
         $('.theme-toggle-button').click(function() {
             const currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
             const newTheme = currentTheme === 'light' ? 'dark' : 'light';
             const newBgColor = (newTheme === 'light') ? 'light' : 'dark';
             applyBackgroundColor(newBgColor);
             $('#bgColor').val(newBgColor);
         });
         */
    }

    /**
     * Toggle settings panel visibility
     * (From reading.js)
     * @param {boolean} [show] - Force show/hide, toggles if undefined
     */
    function toggleSettings(show) {
        const shouldShow = (show === undefined) ? !isSettingsVisible : show;

        if (shouldShow) {
            settingsPanel.addClass('active');
            // Hide hotkey info if open
            if (isHotkeyInfoVisible) {
                toggleHotkeyInfo(false);
            }
        } else {
            settingsPanel.removeClass('active');
        }
        isSettingsVisible = shouldShow;
    }

    /**
     * Toggle hotkey info panel visibility
     * (From reading.js)
     * @param {boolean} [show] - Force show/hide, toggles if undefined
     */
    function toggleHotkeyInfo(show) {
        const shouldShow = (show === undefined) ? !isHotkeyInfoVisible : show;

        if (shouldShow) {
            hotkeyInfoPanel.addClass('show');
             // Hide settings if open
            if (isSettingsVisible) {
                toggleSettings(false);
            }
        } else {
            hotkeyInfoPanel.removeClass('show');
        }
        isHotkeyInfoVisible = shouldShow;
    }


    /**
     * Initialize keyboard shortcuts
     * (Combined from both files, UPDATED for horizontal reading)
     */
    function initializeKeyboardShortcuts() {
        $(document).keydown(function(e) {
            // Ignore if typing in input/textarea or if modifier keys are pressed
            if ($(e.target).is('input, textarea') || e.metaKey || e.ctrlKey || e.altKey) {
                return;
            }

            let handled = false;
            switch (e.key) { // Use e.key for modern browsers
                case 'ArrowLeft':
                    // Only go to previous chapter if we're not in horizontal mode
                    // or if we're not hovering over the manga pages container
                    if (readingMode !== 'horizontal' || !$('.manga-pages-container').is(':hover')) {
                        const prevLink = $('.prev-chapter-link').attr('href');
                        if (prevLink) {
                            window.location.href = prevLink;
                            handled = true;
                        }
                    }
                    break;
                case 'ArrowRight':
                    // Only go to next chapter if we're not in horizontal mode
                    // or if we're not hovering over the manga pages container
                    if (readingMode !== 'horizontal' || !$('.manga-pages-container').is(':hover')) {
                        const nextLink = $('.next-chapter-link').attr('href');
                        if (nextLink) {
                            window.location.href = nextLink;
                            handled = true;
                        }
                    }
                    break;
                case 's': // Toggle Settings
                case 'S':
                    toggleSettings();
                    handled = true;
                    break;
                case 'h': // Toggle Header/Footer
                case 'H':
                     // Toggle visibility directly
                     isNavVisible = !isNavVisible;
                     header.toggleClass('hidden', !isNavVisible);
                     footer.toggleClass('hidden', !isNavVisible);
                     handled = true;
                    break;
                case '?': // Toggle Hotkey Info (Shift + /)
                     if (e.shiftKey) {
                         toggleHotkeyInfo();
                         handled = true;
                     }
                    break;
                 // Add Page Up/Down for scrolling?
                 case 'PageUp':
                     smoothScrollTo($(window).scrollTop() - window.innerHeight * 0.8);
                     handled = true;
                     break;
                 case 'PageDown':
                 case ' ': // Space bar
                     smoothScrollTo($(window).scrollTop() + window.innerHeight * 0.8);
                     handled = true;
                     break;
                 case 'Home':
                     smoothScrollTo(0);
                     handled = true;
                     break;
                 case 'End':
                     smoothScrollTo(document.body.scrollHeight);
                     handled = true;
                     break;
            }

            if (handled) {
                e.preventDefault(); // Prevent default browser action for handled keys
            }
        });
    }

    /**
     * Initialize scroll to top and scroll to bottom buttons
     * (From manga-reader.js - uses smooth scroll)
     */
    function initializeScrollButtons() {
        $('#scrollToTopBtn').click(function() {
            smoothScrollTo(0); // Use smooth scroll
        });

        $('#scrollToBottomBtn').click(function() {
            smoothScrollTo(scrollingElement.scrollHeight - window.innerHeight); // Scroll to bottom
        });
    }

     /**
     * Initialize smooth scrolling functionality
     * (From reading.js)
     */
    function initializeSmoothScroll() {
        function smoothScrollStep() {
            // Check if target is reached (within a small threshold)
            if (Math.abs(targetScrollPosition - currentScrollPosition) < 1) {
                currentScrollPosition = targetScrollPosition; // Snap to final position
                scrollingElement.scrollTop = currentScrollPosition;
                isScrolling = false;
                cancelAnimationFrame(scrollAnimationId);
                return;
            }

            // Calculate the next position (easing effect)
            currentScrollPosition += (targetScrollPosition - currentScrollPosition) * 0.15; // Adjust easing factor (0.1 to 0.3)
            scrollingElement.scrollTop = Math.round(currentScrollPosition); // Use integer values for scrollTop

            // Continue animation if scrolling is still active
            if (isScrolling) {
                scrollAnimationId = requestAnimationFrame(smoothScrollStep);
            }
        }

        // Expose the function to trigger smooth scroll
        window.smoothScrollTo = function (topPosition) {
             // Clamp target position within document bounds
            targetScrollPosition = Math.max(0, Math.min(topPosition, scrollingElement.scrollHeight - window.innerHeight));

            if (!isScrolling) {
                isScrolling = true;
                // Update current position before starting animation
                currentScrollPosition = scrollingElement.scrollTop;
                // Cancel any previous animation frame just in case
                cancelAnimationFrame(scrollAnimationId);
                scrollAnimationId = requestAnimationFrame(smoothScrollStep);
            } else {
                 // If already scrolling, just update the target position
                 // The animation loop will pick up the new target
            }
        };
    }


    /**
     * Record a view for the manga and chapter using primary API
     * (From manga-reader.js)
     */
    function recordView(mangaId, chapterId) {
        if (!mangaId || !chapterId || !isAuthenticated) {
             console.log('View recording skipped (missing data or not authenticated).');
             return; // Don't record if not logged in or data missing
        }

        console.log('Recording view via /api/MangaStatistics/view/...');
        $.ajax({
            url: `/api/MangaStatistics/view/${mangaId}/${chapterId}`,
            type: 'POST',
            success: function(response) {
                console.log('View recorded successfully:', response);
            },
            error: function(xhr, status, error) {
                console.error('Error recording view via primary API:', error, xhr.responseText);
                // Optionally try fallback API on specific errors (e.g., 404 Not Found)
                if (xhr.status === 404 || xhr.status === 500) {
                     console.log('Primary view recording failed, trying fallback...');
                     fallbackRecordView(mangaId, chapterId);
                }
            }
        });
    }

    /**
     * Fallback method to record a view using secondary API
     * (From manga-reader.js)
     */
    function fallbackRecordView(mangaId, chapterId) {
         if (!mangaId || !chapterId || !isAuthenticated) return;

        console.log('Recording view via fallback /api/MangaTracking/view/...');
        $.ajax({
            url: `/api/MangaTracking/view/${mangaId}/${chapterId}`, // Fallback endpoint
            type: 'POST',
            success: function(response) {
                console.log('View recorded successfully (fallback):', response);
            },
            error: function(xhr, status, error) {
                console.error('Error recording view (fallback API):', error, xhr.responseText);
            }
        });
    }

    // Public API (optional, if needed by other scripts)
    // return {
    //     init: initializeReader,
    //     getCurrentPage: () => currentPage,
    //     // Add other public methods if necessary
    // };

    // Initialize on document ready
    $(document).ready(initializeReader);

    // Return something minimal or the init function itself if needed globally
    return {
        init: initializeReader,
        // Expose notify if utility.js isn't globally available and needed
        notify: typeof showToast === 'function' ? showToast : (message, type) => console.log(`[${type}] ${message}`)
    };

})(jQuery); // Pass jQuery to the IIFE
