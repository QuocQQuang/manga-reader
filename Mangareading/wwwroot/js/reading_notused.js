$(document).ready(function () {
    // Variables
    let lastScrollTop = 0;
    let isNavVisible = true;
    let isSettingsVisible = false;
    let isHotkeyInfoVisible = false;
    const header = $('#readingHeader');
    const footer = $('#readingFooter');
    const settings = $('.reading-settings');
    const hotkeyInfo = $('.hotkey-info');

    // Initialization
    loadUserSettings();
    // initializePageVisibility(); // Removed: Lazy loading is now handled by lazy-loader.js

    // Event listener for scroll to hide/show navigation bars
    $(window).scroll(function () {
        const st = $(this).scrollTop();

        // Ignore small scrolls (less than 5px)
        if (Math.abs(lastScrollTop - st) < 5)
            return;

        // Scrolling down
        if (st > lastScrollTop && st > 100) {
            if (isNavVisible) {
                // Hide header first, then footer with a small delay
                header.addClass('hidden');

                setTimeout(function() {
                    footer.addClass('hidden');
                }, 50);

                isNavVisible = false;
            }
        }
        // Scrolling up
        else {
            if (!isNavVisible) {
                // Show header first, then footer with a small delay
                header.removeClass('hidden');

                setTimeout(function() {
                    footer.removeClass('hidden');
                }, 50);

                isNavVisible = true;
            }
        }

        lastScrollTop = st;

        // Update reading progress
        updateReadingProgress();
    });

    // Update reading progress
    function updateReadingProgress() {
        const windowHeight = $(window).height();
        const documentHeight = $(document).height();
        const scrollTop = $(window).scrollTop();

        // Calculate percentage
        const scrollPercent = Math.round((scrollTop / (documentHeight - windowHeight)) * 100);

        $('#readingProgressValue').text(scrollPercent + '%');

        // Find current page in view
        $('.manga-page').each(function () {
            const pageTop = $(this).offset().top;
            const pageHeight = $(this).height();
            const viewportMiddle = scrollTop + (windowHeight / 2);

            if (pageTop <= viewportMiddle && (pageTop + pageHeight) >= viewportMiddle) {
                const pageNum = $(this).data('page');
                $('#currentPageNumber').text('Trang ' + pageNum);

                // Save reading progress
                saveReadingProgress(pageNum);
                return false; // Break the loop
            }
        });
    }

    // Initialize pages visibility with lazy loading
    // DEPRECATED (2025-04-22): Logic moved to wwwroot/js/components/lazy-loader.js
    // function initializePageVisibility() { ... }

    // Handle image loading errors
    // DEPRECATED (2025-04-22): Logic moved to wwwroot/js/components/lazy-loader.js
    // function handleImageError(imgElement, imgSrc, pageNumber) { ... }

    // Save reading progress to localStorage
    function saveReadingProgress(pageNumber) {
        const chapterId = $('#chapterId').val();
        localStorage.setItem('reading_' + chapterId, pageNumber);

        // Update sync status indicator
        updateSyncStatusIndicator('saving');

        // Optional: Save to server via AJAX if user is logged in
        const isLoggedIn = $('#isLoggedIn').val() === 'true';
        if (isLoggedIn) {
            // Throttle server updates to avoid too many requests
            if (window.saveProgressTimeout) {
                clearTimeout(window.saveProgressTimeout);
            }

            window.saveProgressTimeout = setTimeout(() => {
                // Try to use MangaTracking if available, otherwise use fallback
                if (typeof MangaTracking !== 'undefined') {
                    MangaTracking.updateReadingProgress(
                        $('#mangaId').val(),
                        chapterId,
                        pageNumber,
                        $('.manga-page').length
                    );

                    // Check if server sync is available or not
                    setTimeout(() => {
                        if (localStorage.getItem('server_progress_unavailable') === 'true') {
                            updateSyncStatusIndicator('local');
                        } else {
                            updateSyncStatusIndicator('synced');
                        }
                    }, 1000);
                } else {
                    // Fallback to direct API call
                    $.ajax({
                        url: '/api/MangaTracking/progress',
                        method: 'POST',
                        contentType: 'application/json',
                        data: JSON.stringify({
                            mangaId: $('#mangaId').val(),
                            chapterId: chapterId,
                            currentPage: pageNumber,
                            totalPages: $('.manga-page').length,
                            progressPercent: Math.round((pageNumber / $('.manga-page').length) * 100)
                        }),
                        success: function() {
                            updateSyncStatusIndicator('synced');
                        },
                        error: function() {
                            updateSyncStatusIndicator('local');
                        }
                    });
                }
            }, 2000); // Wait 2 seconds before saving to server
        } else {
            // Not logged in, just show local storage indicator
            updateSyncStatusIndicator('local');
        }
    }

    /**
     * Update the sync status indicator
     */
    function updateSyncStatusIndicator(status) {
        const syncStatus = $('#syncStatus');

        // Remove all classes and add base class
        syncStatus.removeClass('text-primary text-success text-warning');

        // Update icon and color based on status
        switch (status) {
            case 'saving':
                syncStatus.html('<i class="fas fa-sync fa-spin"></i>');
                syncStatus.addClass('text-primary');
                syncStatus.attr('title', 'Đang lưu tiến trình...');
                break;
            case 'synced':
                syncStatus.html('<i class="fas fa-cloud-upload-alt"></i>');
                syncStatus.addClass('text-success');
                syncStatus.attr('title', 'Tiến trình đã được đồng bộ');
                break;
            case 'local':
                syncStatus.html('<i class="fas fa-save"></i>');
                syncStatus.addClass('text-warning');
                syncStatus.attr('title', 'Tiến trình được lưu cục bộ');
                break;
            default:
                syncStatus.html('<i class="fas fa-save"></i>');
                syncStatus.attr('title', 'Trạng thái đồng bộ');
        }
    }

    // Load user settings from localStorage
    function loadUserSettings() {
        // Reading mode
        const readingMode = localStorage.getItem('readingMode') || 'vertical';
        $('#readingMode').val(readingMode);
        applyReadingMode(readingMode);

        // Page fit
        const pageFit = localStorage.getItem('pageFit') || 'width';
        $('#pageFit').val(pageFit);
        applyPageFit(pageFit);

        // Background color
        const bgColor = localStorage.getItem('bgColor') || 'dark';
        $('#bgColor').val(bgColor);
        applyBackgroundColor(bgColor);

        // Restore last reading position
        const chapterId = $('#chapterId').val();
        const lastPage = localStorage.getItem('reading_' + chapterId);
        if (lastPage) {
            const pageElement = $('[data-page="' + lastPage + '"]');
            if (pageElement.length) {
                setTimeout(() => {
                    $('html, body').animate({
                        scrollTop: pageElement.offset().top - 100
                    }, 100);
                }, 300);
            }
        }
    }

    // Apply reading mode
    function applyReadingMode(mode) {
        $('.reading-content').removeClass('mode-vertical mode-horizontal mode-webtoon');

        switch (mode) {
            case 'horizontal':
                $('.reading-content').addClass('mode-horizontal');
                // Implement horizontal reading logic
                break;
            case 'webtoon':
                $('.reading-content').addClass('mode-webtoon');
                // Implement webtoon mode logic
                break;
            default:
                $('.reading-content').addClass('mode-vertical');
        }

        localStorage.setItem('readingMode', mode);
    }

    // Apply page fit
    function applyPageFit(fit) {
        $('.manga-page').removeClass('fit-width fit-height fit-original');

        switch (fit) {
            case 'height':
                $('.manga-page').addClass('fit-height');
                break;
            case 'original':
                $('.manga-page').addClass('fit-original');
                break;
            default:
                $('.manga-page').addClass('fit-width');
        }

        localStorage.setItem('pageFit', fit);
    }

    // Apply background color
    function applyBackgroundColor(color) {
        // First remove legacy background color classes
        $('body').removeClass('bg-dark bg-black bg-gray bg-light');

        // Set theme based on background color choice
        let themeValue;
        switch (color) {
            case 'light':
                themeValue = 'light';
                break;
            case 'dark':
            case 'black':
            case 'gray':
            default:
                themeValue = 'dark';
                break;
        }

        // Apply legacy class for backward compatibility
        $('body').addClass('bg-' + color);
        
        // Update theme in HTML attribute and localStorage
        document.documentElement.setAttribute('data-theme', themeValue);
        localStorage.setItem('theme', themeValue);
        localStorage.setItem('bgColor', color);
        
        // Update theme toggle icons to match current theme
        updateThemeToggleIcons(themeValue);
    }
    
    // Update theme toggle icons to match the current theme
    function updateThemeToggleIcons(theme) {
        const themeToggleButtons = $('#themeToggleBtn, #themeToggleBtnBottom, #themeToggleBtnTop');
        
        themeToggleButtons.each(function() {
            const icon = $(this).find('i');
            if (icon.length > 0) {
                if (theme === 'dark') {
                    icon.removeClass('fa-sun');
                    icon.addClass('fa-moon');
                } else {
                    icon.removeClass('fa-moon');
                    icon.addClass('fa-sun');
                }
            }
        });
    }

    // Event listeners for settings controls
    $('#readingMode').change(function () {
        applyReadingMode($(this).val());
    });

    $('#pageFit').change(function () {
        applyPageFit($(this).val());
    });

    $('#bgColor').change(function () {
        applyBackgroundColor($(this).val());
    });

    // Also listen for theme changes from theme-switcher.js
    $(document).on('themeChanged', function(e, newTheme) {
        // Map theme to background color
        let bgColor;
        switch(newTheme) {
            case 'light':
                bgColor = 'light';
                break;
            case 'dark':
            default:
                bgColor = 'dark';
                break;
        }
        
        // Update the bgColor select and apply the color
        $('#bgColor').val(bgColor);
        applyBackgroundColor(bgColor);
    });

    // Toggle settings panel
    $('#settingsToggle, #settingsToggleBottom').click(function () {
        toggleSettings();
    });

    $('#closeSettings').click(function () {
        toggleSettings();
    });

    function toggleSettings() {
        if (isSettingsVisible) {
            settings.removeClass('active');
        } else {
            settings.addClass('active');
            hotkeyInfo.removeClass('show');
            isHotkeyInfoVisible = false;
        }

        isSettingsVisible = !isSettingsVisible;
    }

    // Toggle hotkey info
    $('#hotkeyToggle').click(function () {
        toggleHotkeyInfo();
    });

    function toggleHotkeyInfo() {
        if (isHotkeyInfoVisible) {
            hotkeyInfo.removeClass('show');
        } else {
            hotkeyInfo.addClass('show');
            settings.removeClass('active');
            isSettingsVisible = false;
        }

        isHotkeyInfoVisible = !isHotkeyInfoVisible;
    }

    // Keyboard shortcuts
    $(document).keydown(function (e) {
        // Left arrow key - previous chapter
        if (e.keyCode === 37) {
            const prevLink = $('.prev-chapter-link').attr('href');
            if (prevLink) {
                window.location.href = prevLink;
            }
        }

        // Right arrow key - next chapter
        if (e.keyCode === 39) {
            const nextLink = $('.next-chapter-link').attr('href');
            if (nextLink) {
                window.location.href = nextLink;
            }
        }

        // S key - toggle settings
        if (e.keyCode === 83) {
            toggleSettings();
        }

        // H key - toggle navigation bars
        if (e.keyCode === 72) {
            if (isNavVisible) {
                // Hide header first, then footer with a small delay
                header.addClass('hidden');

                setTimeout(function() {
                    footer.addClass('hidden');
                }, 50);

                isNavVisible = false;
            } else {
                // Show header first, then footer with a small delay
                header.removeClass('hidden');

                setTimeout(function() {
                    footer.removeClass('hidden');
                }, 50);

                isNavVisible = true;
            }
        }

        // ? key - toggle hotkey info
        if (e.keyCode === 191 && e.shiftKey) {
            toggleHotkeyInfo();
        }
    });

    // Initialize network monitor
    document.addEventListener('DOMContentLoaded', function () {
        MangaReader.initNetworkMonitor();

        // Show welcome message
        setTimeout(() => {
            const currentUser = document.querySelector('.user-badge span')?.textContent || 'QuocQQuangtiep';
            MangaReader.notify(`Chào mừng ${currentUser}! Chúc bạn có trải nghiệm đọc truyện tốt!`, 'info', 4000);
        }, 2000);
    });

    // Add smooth scrolling feature
    (function () {
        let isScrolling = false;
        let scrollingElement = document.scrollingElement || document.documentElement;
        let currentPosition = scrollingElement.scrollTop;
        let targetPosition = currentPosition;
        let scrollAnimationId = null;

        function smoothScroll() {
            if (Math.abs(targetPosition - currentPosition) < 1) {
                currentPosition = targetPosition;
                isScrolling = false;
                cancelAnimationFrame(scrollAnimationId);
                return;
            }

            currentPosition += (targetPosition - currentPosition) * 0.1;
            scrollingElement.scrollTop = currentPosition;
            scrollAnimationId = requestAnimationFrame(smoothScroll);
        }

        window.smoothScrollTo = function (top) {
            targetPosition = top;
            if (!isScrolling) {
                isScrolling = true;
                currentPosition = scrollingElement.scrollTop;
                scrollAnimationId = requestAnimationFrame(smoothScroll);
            }
        };
    })();
});