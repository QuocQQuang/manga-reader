/**
 * Reading Layout JavaScript
 * Handles functionality specific to the reading layout of the Manga Reader application
 */

class ReadingLayout {
    /**
     * Initialize the reading layout
     */
    constructor() {
        this.settings = {
            readingMode: 'vertical', // vertical, horizontal, webtoon
            pageFit: 'width', // width, height, original
            bgColor: 'dark' // dark, black, gray, light
        };
        
        this.elements = {
            controls: null,
            settings: null,
            hotkeyInfo: null,
            navigation: null,
            content: null,
            pages: null
        };
        
        this.state = {
            controlsVisible: true,
            settingsVisible: false,
            hotkeyInfoVisible: false,
            lastScrollPosition: 0,
            scrollTimer: null,
            touchStartY: 0,
            touchStartX: 0
        };
        
        this.init();
    }
    
    /**
     * Initialize layout functionality
     */
    init() {
        document.addEventListener('DOMContentLoaded', () => {
            this.cacheElements();
            this.loadSettings();
            this.applySettings();
            this.setupEventListeners();
            this.setupKeyboardShortcuts();
            this.setupTouchControls();
            this.setupScrollHandler();
            this.setupPageTracking(); // Add page tracking
            this.recordInitialView(); // Record view on load
        });
    }
    
    /**
     * Cache DOM elements
     */
    cacheElements() {
        this.elements.controls = document.querySelector('.reading-controls');
        this.elements.settings = document.getElementById('readingSettings');
        this.elements.hotkeyInfo = document.getElementById('hotkeyInfo');
        this.elements.navigation = document.querySelector('.chapter-navigation');
        this.elements.content = document.querySelector('.reading-content');
        this.elements.pages = document.querySelector('.manga-pages');
    }
    
    /**
     * Load saved settings from localStorage
     */
    loadSettings() {
        const savedSettings = localStorage.getItem('readingSettings');
        
        if (savedSettings) {
            try {
                const parsedSettings = JSON.parse(savedSettings);
                this.settings = { ...this.settings, ...parsedSettings };
            } catch (error) {
                console.error('Error loading reading settings:', error);
            }
        }
        
        // Update settings form
        const readingModeSelect = document.getElementById('readingMode');
        const pageFitSelect = document.getElementById('pageFit');
        const bgColorSelect = document.getElementById('bgColor');
        
        if (readingModeSelect) readingModeSelect.value = this.settings.readingMode;
        if (pageFitSelect) pageFitSelect.value = this.settings.pageFit;
        if (bgColorSelect) bgColorSelect.value = this.settings.bgColor;
    }
    
    /**
     * Save settings to localStorage
     */
    saveSettings() {
        localStorage.setItem('readingSettings', JSON.stringify(this.settings));
    }
    
    /**
     * Apply current settings to the UI
     */
    applySettings() {
        // Remove all setting classes
        document.body.classList.remove(
            'reading-mode-vertical', 'reading-mode-horizontal', 'reading-mode-webtoon',
            'page-fit-width', 'page-fit-height', 'page-fit-original',
            'bg-color-dark', 'bg-color-black', 'bg-color-gray', 'bg-color-light'
        );
        
        // Apply reading mode
        document.body.classList.add(`reading-mode-${this.settings.readingMode}`);
        
        // Apply page fit
        document.body.classList.add(`page-fit-${this.settings.pageFit}`);
        
        // Apply background color
        document.body.classList.add(`bg-color-${this.settings.bgColor}`);
    }
    
    /**
     * Set up event listeners
     */
    setupEventListeners() {
        // Settings form
        const closeSettingsBtn = document.getElementById('closeSettings');
        const readingModeSelect = document.getElementById('readingMode');
        const pageFitSelect = document.getElementById('pageFit');
        const bgColorSelect = document.getElementById('bgColor');
        
        if (closeSettingsBtn) {
            closeSettingsBtn.addEventListener('click', () => {
                this.toggleSettings(false);
                
                // Get values from form
                if (readingModeSelect) this.settings.readingMode = readingModeSelect.value;
                if (pageFitSelect) this.settings.pageFit = pageFitSelect.value;
                if (bgColorSelect) this.settings.bgColor = bgColorSelect.value;
                
                // Apply and save settings
                this.applySettings();
                this.saveSettings();
            });
        }
        
        // Chapter navigation
        const prevChapterBtn = document.querySelector('.chapter-navigation-btn.prev');
        const nextChapterBtn = document.querySelector('.chapter-navigation-btn.next');
        
        if (prevChapterBtn) {
            prevChapterBtn.addEventListener('click', () => {
                const prevChapterUrl = prevChapterBtn.getAttribute('data-url');
                if (prevChapterUrl) {
                    window.location.href = prevChapterUrl;
                }
            });
        }
        
        if (nextChapterBtn) {
            nextChapterBtn.addEventListener('click', () => {
                const nextChapterUrl = nextChapterBtn.getAttribute('data-url');
                if (nextChapterUrl) {
                    window.location.href = nextChapterUrl;
                }
            });
        }
        
        // Chapter selector
        const chapterSelectorToggle = document.querySelector('.chapter-selector-toggle');
        const chapterSelectorDropdown = document.querySelector('.chapter-selector-dropdown');
        
        if (chapterSelectorToggle && chapterSelectorDropdown) {
            chapterSelectorToggle.addEventListener('click', () => {
                chapterSelectorDropdown.classList.toggle('show');
            });
            
            // Close dropdown when clicking outside
            document.addEventListener('click', (e) => {
                if (!chapterSelectorToggle.contains(e.target) && !chapterSelectorDropdown.contains(e.target)) {
                    chapterSelectorDropdown.classList.remove('show');
                }
            });
            
            // Chapter selection
            const chapterItems = chapterSelectorDropdown.querySelectorAll('.chapter-selector-item');
            chapterItems.forEach(item => {
                item.addEventListener('click', () => {
                    const chapterUrl = item.getAttribute('data-url');
                    if (chapterUrl) {
                        window.location.href = chapterUrl;
                    }
                });
            });
        }
    }
    
    /**
     * Set up keyboard shortcuts
     */
    setupKeyboardShortcuts() {
        document.addEventListener('keydown', (e) => {
            // Ignore if typing in an input
            if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') {
                return;
            }
            
            switch (e.key) {
                case 'ArrowLeft':
                    // Previous chapter
                    const prevChapterBtn = document.querySelector('.chapter-navigation-btn.prev');
                    if (prevChapterBtn && !prevChapterBtn.classList.contains('disabled')) {
                        prevChapterBtn.click();
                    }
                    break;
                    
                case 'ArrowRight':
                    // Next chapter
                    const nextChapterBtn = document.querySelector('.chapter-navigation-btn.next');
                    if (nextChapterBtn && !nextChapterBtn.classList.contains('disabled')) {
                        nextChapterBtn.click();
                    }
                    break;
                    
                case 's':
                case 'S':
                    // Toggle settings
                    this.toggleSettings();
                    break;
                    
                case 'h':
                case 'H':
                    // Toggle controls
                    this.toggleControls();
                    break;
                    
                case '?':
                    // Toggle hotkey info
                    this.toggleHotkeyInfo();
                    break;
            }
        });
    }
    
    /**
     * Set up touch controls
     */
    setupTouchControls() {
        // Detect swipe gestures
        document.addEventListener('touchstart', (e) => {
            this.state.touchStartY = e.touches[0].clientY;
            this.state.touchStartX = e.touches[0].clientX;
        });
        
        document.addEventListener('touchend', (e) => {
            const touchEndY = e.changedTouches[0].clientY;
            const touchEndX = e.changedTouches[0].clientX;
            
            const diffY = this.state.touchStartY - touchEndY;
            const diffX = this.state.touchStartX - touchEndX;
            
            // Horizontal swipe detection (for chapter navigation)
            if (Math.abs(diffX) > 100 && Math.abs(diffY) < 50) {
                if (diffX > 0) {
                    // Swipe left (next chapter)
                    const nextChapterBtn = document.querySelector('.chapter-navigation-btn.next');
                    if (nextChapterBtn && !nextChapterBtn.classList.contains('disabled')) {
                        nextChapterBtn.click();
                    }
                } else {
                    // Swipe right (previous chapter)
                    const prevChapterBtn = document.querySelector('.chapter-navigation-btn.prev');
                    if (prevChapterBtn && !prevChapterBtn.classList.contains('disabled')) {
                        prevChapterBtn.click();
                    }
                }
            }
            
            // Vertical swipe detection (for showing/hiding controls)
            if (Math.abs(diffY) > 100 && Math.abs(diffX) < 50) {
                if (diffY > 0) {
                    // Swipe up (hide controls)
                    this.toggleControls(false);
                } else {
                    // Swipe down (show controls)
                    this.toggleControls(true);
                }
            }
        });
        
        // Single tap to toggle controls
        document.addEventListener('click', (e) => {
            // Ignore if clicking on a button or link
            if (e.target.tagName === 'BUTTON' || e.target.tagName === 'A' || 
                e.target.closest('button') || e.target.closest('a')) {
                return;
            }
            
            // Toggle controls on tap
            this.toggleControls();
        });
    }
    
    /**
     * Set up scroll handler to hide/show controls
     */
    setupScrollHandler() {
        window.addEventListener('scroll', () => {
            const currentScrollPosition = window.scrollY;
            
            // Clear previous timer
            clearTimeout(this.state.scrollTimer);
            
            // Determine scroll direction
            if (currentScrollPosition > this.state.lastScrollPosition + 50) {
                // Scrolling down, hide controls
                this.toggleControls(false);
            } else if (currentScrollPosition < this.state.lastScrollPosition - 50) {
                // Scrolling up, show controls
                this.toggleControls(true);
            }
            
            // Update last scroll position
            this.state.lastScrollPosition = currentScrollPosition;
            
            // Set timer to show controls when scrolling stops
            this.state.scrollTimer = setTimeout(() => {
                this.toggleControls(true);
            }, 2000);
        });
    }
    
    /**
     * Set up IntersectionObserver to track visible pages
     */
    setupPageTracking() {
        const pageImages = document.querySelectorAll('.manga-page[data-page]');
        const totalPages = pageImages.length;
        const mangaId = document.getElementById('mangaId')?.value;
        const chapterId = document.getElementById('chapterId')?.value;
        const currentPageElement = document.getElementById('currentPageNumber');

        if (!pageImages.length || !mangaId || !chapterId || !currentPageElement) {
            console.warn('Page tracking setup failed: Missing elements or data.');
            return;
        }

        const observerOptions = {
            root: null, // viewport
            rootMargin: '0px',
            threshold: 0.5 // Trigger when 50% of the image is visible
        };

        let lastTrackedPage = 0;

        const observerCallback = (entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const pageNumber = parseInt(entry.target.getAttribute('data-page'), 10);
                    
                    // Update UI
                    currentPageElement.textContent = `Trang ${pageNumber}`;

                    // Update progress only if the page number has changed significantly
                    // and user is authenticated
                    if (pageNumber > lastTrackedPage && window.MangaTracking && typeof window.MangaTracking.updateReadingProgress === 'function') {
                        lastTrackedPage = pageNumber;
                        // Debounce the update call slightly
                        clearTimeout(this.updateProgressTimeout);
                        this.updateProgressTimeout = setTimeout(() => {
                            window.MangaTracking.updateReadingProgress(mangaId, chapterId, pageNumber, totalPages);
                        }, 500); // 500ms debounce
                    }
                }
            });
        };

        const observer = new IntersectionObserver(observerCallback, observerOptions);
        pageImages.forEach(img => observer.observe(img));
    }

    /**
     * Record the initial view for the chapter
     */
    recordInitialView() {
        const mangaId = document.getElementById('mangaId')?.value;
        const chapterId = document.getElementById('chapterId')?.value;

        if (mangaId && chapterId && window.MangaTracking && typeof window.MangaTracking.recordView === 'function') {
            // Delay slightly to ensure MangaTracking is fully initialized
            setTimeout(() => {
                 window.MangaTracking.recordView(mangaId, chapterId);
            }, 1000);
        }
    }

    /**
     * Toggle settings panel
     * param {boolean} show - Whether to show the settings
     */
    toggleSettings(show) {
        if (!this.elements.settings) return;
        
        if (show === undefined) {
            show = !this.elements.settings.classList.contains('show');
        }
        
        if (show) {
            this.elements.settings.classList.add('show');
            this.state.settingsVisible = true;
        } else {
            this.elements.settings.classList.remove('show');
            this.state.settingsVisible = false;
        }
    }
    
    /**
     * Toggle controls visibility
     * param {boolean} show - Whether to show the controls
     */
    toggleControls(show) {
        if (!this.elements.controls || !this.elements.navigation) return;
        
        if (show === undefined) {
            show = this.elements.controls.classList.contains('hidden');
        }
        
        if (show) {
            this.elements.controls.classList.remove('hidden');
            this.elements.navigation.classList.remove('hidden');
            this.state.controlsVisible = true;
        } else {
            this.elements.controls.classList.add('hidden');
            this.elements.navigation.classList.add('hidden');
            this.state.controlsVisible = false;
        }
    }
    
    /**
     * Toggle hotkey info panel
     * param {boolean} show - Whether to show the hotkey info
     */
    toggleHotkeyInfo(show) {
        if (!this.elements.hotkeyInfo) return;
        
        if (show === undefined) {
            show = !this.elements.hotkeyInfo.classList.contains('show');
        }
        
        if (show) {
            this.elements.hotkeyInfo.classList.add('show');
            this.state.hotkeyInfoVisible = true;
        } else {
            this.elements.hotkeyInfo.classList.remove('show');
            this.state.hotkeyInfoVisible = false;
        }
    }

    /**
     * Update the sync status icon in the popup
     * @param {'idle' | 'syncing' | 'synced' | 'error'} status 
     */
    updateSyncStatusIcon(status) {
        const syncIcon = document.querySelector('#syncStatus i');
        if (!syncIcon) return;

        syncIcon.classList.remove('syncing', 'synced', 'error'); // Remove all status classes
        syncIcon.style.animation = ''; // Remove animation

        switch (status) {
            case 'syncing':
                syncIcon.classList.add('syncing');
                syncIcon.style.animation = 'spin 1s linear infinite'; // Re-apply animation
                syncIcon.title = 'Đang đồng bộ...';
                break;
            case 'synced':
                syncIcon.classList.add('synced');
                syncIcon.title = 'Đã đồng bộ';
                break;
            case 'error':
                syncIcon.classList.add('error');
                syncIcon.title = 'Lỗi đồng bộ hóa';
                break;
            case 'idle':
            default:
                 syncIcon.title = 'Tiến trình được lưu cục bộ';
                // Keep default grey color, no extra class needed
                break;
        }
    }
}

// Initialize reading layout
const readingLayout = new ReadingLayout();

// Export the ReadingLayout class and instance
window.ReadingLayout = ReadingLayout;
window.readingLayout = readingLayout;
