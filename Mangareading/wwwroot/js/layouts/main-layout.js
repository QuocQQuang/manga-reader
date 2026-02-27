/**
 * Main Layout JavaScript
 * Handles functionality specific to the main layout of the Manga Reader application
 */

class MainLayout {
    /**
     * Initialize the main layout
     */
    constructor() {
        this.init();
    }
    
    /**
     * Initialize layout functionality
     */
    init() {
        document.addEventListener('DOMContentLoaded', () => {
            this.initializeTooltips();
            this.initializePopovers();
            this.initializeDropdowns();
            this.initializeScrollProgress();
            this.initializeBackToTop();
            this.initializeAnimations();
        });
    }
    
    /**
     * Initialize Bootstrap tooltips
     */
    initializeTooltips() {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }
    
    /**
     * Initialize Bootstrap popovers
     */
    initializePopovers() {
        const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
        popoverTriggerList.map(function (popoverTriggerEl) {
            return new bootstrap.Popover(popoverTriggerEl);
        });
    }
    
    /**
     * Initialize custom dropdowns
     */
    initializeDropdowns() {
        // User dropdown toggle
        const userDropdownToggle = document.getElementById('userDropdown');
        const userDropdownMenu = document.querySelector('.user-dropdown-menu');
        
        if (userDropdownToggle && userDropdownMenu) {
            userDropdownToggle.addEventListener('click', (e) => {
                e.preventDefault();
                userDropdownMenu.classList.toggle('show');
            });
            
            // Close dropdown when clicking outside
            document.addEventListener('click', (e) => {
                if (!userDropdownToggle.contains(e.target) && !userDropdownMenu.contains(e.target)) {
                    userDropdownMenu.classList.remove('show');
                }
            });
        }
    }
    
    /**
     * Initialize scroll progress indicator
     */
    initializeScrollProgress() {
        const scrollProgress = document.getElementById('scrollProgress');
        
        if (scrollProgress) {
            window.addEventListener('scroll', () => {
                const scrollTop = window.scrollY;
                const docHeight = document.documentElement.scrollHeight - window.innerHeight;
                const scrollPercent = (scrollTop / docHeight) * 100;
                
                scrollProgress.style.width = scrollPercent + '%';
            });
        }
    }
    
    /**
     * Initialize back to top button
     */
    initializeBackToTop() {
        const backToTopBtn = document.getElementById('backToTopBtn');
        
        if (backToTopBtn) {
            window.addEventListener('scroll', () => {
                if (window.scrollY > 300) {
                    backToTopBtn.classList.add('show');
                } else {
                    backToTopBtn.classList.remove('show');
                }
            });
            
            backToTopBtn.addEventListener('click', (e) => {
                e.preventDefault();
                window.scrollTo({
                    top: 0,
                    behavior: 'smooth'
                });
            });
        }
    }
    
    /**
     * Initialize scroll animations
     */
    initializeAnimations() {
        const animateElements = document.querySelectorAll('.animate-on-scroll');
        
        if (animateElements.length > 0) {
            const animateOnScroll = () => {
                animateElements.forEach(element => {
                    const elementTop = element.getBoundingClientRect().top;
                    const windowHeight = window.innerHeight;
                    
                    if (elementTop < windowHeight * 0.85) {
                        element.classList.add('animated');
                    }
                });
            };
            
            // Run once on page load
            animateOnScroll();
            
            // And again on scroll
            window.addEventListener('scroll', animateOnScroll);
        }
    }
}

// Initialize main layout
const mainLayout = new MainLayout();

// Export the MainLayout class and instance
window.MainLayout = MainLayout;
window.mainLayout = mainLayout;
