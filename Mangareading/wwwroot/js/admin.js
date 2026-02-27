// Admin Dashboard JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // Initialize tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Initialize sidebar toggle
    initSidebar();
    
    // Initialize theme toggle icon based on current theme
    initThemeToggleIcon();
    
    // Apply scrollbar width fix
    fixScrollbarShift();

    // Initialize DataTables if available
    if (typeof $.fn.DataTable !== 'undefined') {
        $('.data-table table').DataTable({
            responsive: true,
            language: {
                search: "Tìm kiếm:",
                lengthMenu: "Hiển thị _MENU_ mục",
                info: "Hiển thị _START_ đến _END_ của _TOTAL_ mục",
                infoEmpty: "Hiển thị 0 đến 0 của 0 mục",
                infoFiltered: "(lọc từ _MAX_ mục)",
                paginate: {
                    first: "Đầu",
                    previous: "Trước",
                    next: "Tiếp",
                    last: "Cuối"
                }
            }
        });
    }

    // Render charts if chart elements are present
    renderCharts();
});

// Function to render charts
function renderCharts() {
    // Users growth chart
    const usersChartEl = document.getElementById('usersGrowthChart');
    if (usersChartEl) {
        const usersChart = new Chart(usersChartEl, {
            type: 'line',
            data: {
                labels: ['Tháng 1', 'Tháng 2', 'Tháng 3', 'Tháng 4', 'Tháng 5', 'Tháng 6'],
                datasets: [{
                    label: 'Người dùng mới',
                    data: [65, 78, 92, 83, 106, 120],
                    fill: false,
                    borderColor: '#4e73df',
                    tension: 0.1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true
                    }
                }
            }
        });
    }

    // Manga views chart
    const mangaViewsChartEl = document.getElementById('mangaViewsChart');
    if (mangaViewsChartEl) {
        const mangaViewsChart = new Chart(mangaViewsChartEl, {
            type: 'bar',
            data: {
                labels: ['Tháng 1', 'Tháng 2', 'Tháng 3', 'Tháng 4', 'Tháng 5', 'Tháng 6'],
                datasets: [{
                    label: 'Lượt xem',
                    data: [1500, 1700, 1850, 2100, 2350, 2800],
                    backgroundColor: '#1cc88a'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    }
                }
            }
        });
    }

    // Favorites distribution chart
    const favoritesChartEl = document.getElementById('favoritesChart');
    if (favoritesChartEl) {
        const favoritesChart = new Chart(favoritesChartEl, {
            type: 'doughnut',
            data: {
                labels: ['Hành động', 'Tình cảm', 'Phiêu lưu', 'Kinh dị', 'Khoa học'],
                datasets: [{
                    data: [35, 20, 25, 10, 10],
                    backgroundColor: [
                        '#4e73df',
                        '#1cc88a',
                        '#36b9cc',
                        '#f6c23e',
                        '#e74a3b'
                    ]
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom'
                    }
                },
                cutout: '70%'
            }
        });
    }
}

// Handle sync manga form submission
const syncForm = document.getElementById('syncMangaForm');
if (syncForm) {
    syncForm.addEventListener('submit', function(e) {
        const limitInput = document.getElementById('limit');
        if (limitInput && (limitInput.value < 1 || limitInput.value > 500)) {
            e.preventDefault();
            alert('Số lượng phải từ 1 đến 500 truyện.');
        }
    });
}

// Handle user management
const userStatusBtns = document.querySelectorAll('.toggle-user-status');
if (userStatusBtns.length > 0) {
    userStatusBtns.forEach(btn => {
        btn.addEventListener('click', function() {
            const userId = this.getAttribute('data-user-id');
            const statusType = this.getAttribute('data-status-type');

            // Here would be AJAX call to update user status
            console.log(`Update user ${userId} status to ${statusType}`);

            // Update UI based on response
            if (statusType === 'block') {
                this.classList.remove('btn-danger');
                this.classList.add('btn-success');
                this.setAttribute('data-status-type', 'unblock');
                this.querySelector('i').classList.remove('fa-ban');
                this.querySelector('i').classList.add('fa-check');
            } else {
                this.classList.remove('btn-success');
                this.classList.add('btn-danger');
                this.setAttribute('data-status-type', 'block');
                this.querySelector('i').classList.remove('fa-check');
                this.querySelector('i').classList.add('fa-ban');
            }
        });
    });
}

// Search functionality
const searchInput = document.getElementById('adminSearch');
const userManagementContainer = document.querySelector('.user-management-container');
const mangaManagementContainer = document.querySelector('.manga-management-container');

if (searchInput) {
    searchInput.addEventListener('keyup', function() {
        const searchTerm = this.value.toLowerCase().trim();
        const tableBody = document.querySelector('.admin-table tbody');
        if (!tableBody) return;

        const tableRows = tableBody.querySelectorAll('tr');

        tableRows.forEach(row => {
            let textToSearch = '';

            // Determine which table we are searching based on container class
            if (userManagementContainer) {
                // User Management: Search Username (index 1) and Email (index 2)
                const usernameCell = row.cells[1];
                const emailCell = row.cells[2];
                if (usernameCell) textToSearch += usernameCell.textContent.toLowerCase().trim();
                if (emailCell) textToSearch += ' ' + emailCell.textContent.toLowerCase().trim();
            } else if (mangaManagementContainer) {
                // Manga Management: Search Title (index 2)
                const titleCell = row.cells[2];
                if (titleCell) textToSearch += titleCell.textContent.toLowerCase().trim();
            } else {
                // Default/Fallback: search all text content (original behavior)
                textToSearch = row.textContent.toLowerCase();
            }

            if (textToSearch.includes(searchTerm)) {
                row.style.display = '';
            } else {
                row.style.display = 'none';
            }
        });

        // Add logic to show/hide "No results" message if needed
        // (Similar to chapter filter, requires a dedicated element)
    });
}

// Initialize sidebar functionality
function initSidebar() {
    const sidebarToggle = document.getElementById('sidebarToggle');
    const body = document.body;
    
    // Check for saved sidebar state
    const sidebarCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';
    
    // Apply initial state
    if (sidebarCollapsed) {
        body.classList.add('sidebar-collapsed');
    }
    
    // Add click event for toggling
    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', function(e) {
            // Add transitioning class to prevent scrollbar flicker
            body.classList.add('sidebar-transitioning');
            
            // Toggle sidebar collapsed state
            body.classList.toggle('sidebar-collapsed');
            
            // Save state to localStorage
            const isCollapsed = body.classList.contains('sidebar-collapsed');
            localStorage.setItem('sidebarCollapsed', isCollapsed);
            
            // Remove transitioning class after animation completes
            setTimeout(function() {
                body.classList.remove('sidebar-transitioning');
            }, 300); // Match with transition duration
        });
    }
}

// Initialize theme toggle icon based on current theme
function initThemeToggleIcon() {
    const themeToggleBtn = document.getElementById('themeToggleBtn');
    if (themeToggleBtn) {
        const currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
        const icon = themeToggleBtn.querySelector('i');
        
        if (icon) {
            if (currentTheme === 'dark') {
                icon.classList.remove('fa-sun');
                icon.classList.add('fa-moon');
            } else {
                icon.classList.remove('fa-moon');
                icon.classList.add('fa-sun');
            }
        }
        
        // Add event listener for theme toggle
        themeToggleBtn.addEventListener('click', function(e) {
            // Prevent event bubbling since this is now in the nav menu
            e.preventDefault();
            e.stopPropagation();
            
            const currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
            const newTheme = currentTheme === 'light' ? 'dark' : 'light';
            
            // Set theme
            document.documentElement.setAttribute('data-theme', newTheme);
            document.body.setAttribute('data-theme', newTheme);
            localStorage.setItem('theme', newTheme);
            
            // Update icon
            const icon = this.querySelector('i');
            if (icon) {
                if (newTheme === 'dark') {
                    icon.classList.remove('fa-sun');
                    icon.classList.add('fa-moon');
                } else {
                    icon.classList.remove('fa-moon');
                    icon.classList.add('fa-sun');
                }
            }
            
            // Emit theme change event
            document.dispatchEvent(new CustomEvent('themeChanged', { 
                bubbles: true,
                detail: { theme: newTheme }
            }));
            
            // Also trigger jQuery event for older components
            if (typeof jQuery !== 'undefined') {
                jQuery(document).trigger('themeChanged', newTheme);
            }
        });
    }
}

// Apply scrollbar width fix
function fixScrollbarShift() {
    // Calculate scrollbar width
    const scrollDiv = document.createElement('div');
    scrollDiv.style.cssText = 'width: 100px; height: 100px; overflow: scroll; position: absolute; top: -9999px;';
    document.body.appendChild(scrollDiv);
    const scrollbarWidth = scrollDiv.offsetWidth - scrollDiv.clientWidth;
    document.body.removeChild(scrollDiv);
    
    // Create a style element to add dynamic CSS
    const styleEl = document.createElement('style');
    document.head.appendChild(styleEl);
    
    // Add CSS rule to handle scrollbar during transitions
    styleEl.sheet.insertRule(`
        .sidebar-transitioning {
            padding-right: ${scrollbarWidth}px !important;
        }
    `, 0);
    
    // Add listener to handle window resize events
    window.addEventListener('resize', function() {
        // Ensure proper width calculation after resize
        if (document.body.classList.contains('sidebar-collapsed')) {
            document.querySelector('.main-content').style.width = `calc(100% - 70px)`;
        } else {
            document.querySelector('.main-content').style.width = `calc(100% - 250px)`;
        }
    });
}