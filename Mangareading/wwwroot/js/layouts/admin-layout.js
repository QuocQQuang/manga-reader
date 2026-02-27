/**
 * Admin Layout JavaScript
 * Handles functionality specific to the admin layout of the Manga Reader application
 */

class AdminLayout {
    /**
     * Initialize the admin layout
     */
    constructor() {
        this.init();
    }
    
    /**
     * Initialize layout functionality
     */
    init() {
        document.addEventListener('DOMContentLoaded', () => {
            this.initializeSidebar();
            this.initializeDropdowns();
            this.initializeCharts();
            this.initializeDataTables();
            this.initializeFormValidation();
        });
    }
    
    /**
     * Initialize sidebar functionality
     */
    initializeSidebar() {
        const sidebarToggle = document.getElementById('sidebarToggle');
        const adminContainer = document.querySelector('.admin-container');
        
        if (sidebarToggle && adminContainer) {
            sidebarToggle.addEventListener('click', () => {
                adminContainer.classList.toggle('sidebar-collapsed');
                
                // Save sidebar state to localStorage
                const isCollapsed = adminContainer.classList.contains('sidebar-collapsed');
                localStorage.setItem('adminSidebarCollapsed', isCollapsed);
            });
            
            // Restore sidebar state from localStorage
            const savedState = localStorage.getItem('adminSidebarCollapsed');
            if (savedState === 'true') {
                adminContainer.classList.add('sidebar-collapsed');
            } else if (savedState === 'false') {
                adminContainer.classList.remove('sidebar-collapsed');
            }
            
            // Close sidebar when clicking outside on mobile
            document.addEventListener('click', (e) => {
                const isMobile = window.innerWidth < 992;
                const isOutsideSidebar = !e.target.closest('.sidebar') && !e.target.closest('#sidebarToggle');
                
                if (isMobile && isOutsideSidebar && adminContainer.classList.contains('sidebar-collapsed')) {
                    adminContainer.classList.remove('sidebar-collapsed');
                    localStorage.setItem('adminSidebarCollapsed', false);
                }
            });
        }
    }
    
    /**
     * Initialize dropdown functionality
     */
    initializeDropdowns() {
        // Notifications dropdown
        const notificationsDropdown = document.getElementById('notificationsDropdown');
        
        if (notificationsDropdown) {
            notificationsDropdown.addEventListener('click', (e) => {
                e.preventDefault();
                const dropdown = notificationsDropdown.nextElementSibling;
                dropdown.classList.toggle('show');
                
                // Close dropdown when clicking outside
                document.addEventListener('click', (event) => {
                    if (!notificationsDropdown.contains(event.target) && !dropdown.contains(event.target)) {
                        dropdown.classList.remove('show');
                    }
                });
            });
        }
    }
    
    /**
     * Initialize Chart.js charts
     */
    initializeCharts() {
        // Check if Chart.js is available
        if (typeof Chart === 'undefined') return;
        
        // Users chart
        const usersChartCanvas = document.getElementById('usersChart');
        if (usersChartCanvas) {
            const usersChart = new Chart(usersChartCanvas, {
                type: 'line',
                data: {
                    labels: this.getLast7Days(),
                    datasets: [{
                        label: 'Người dùng mới',
                        data: this.getRandomData(7, 10, 50),
                        borderColor: '#6366f1',
                        backgroundColor: 'rgba(99, 102, 241, 0.1)',
                        tension: 0.4,
                        fill: true
                    }]
                },
                options: {
                    responsive: true,
                    plugins: {
                        legend: {
                            position: 'top',
                        },
                        tooltip: {
                            mode: 'index',
                            intersect: false
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
        
        // Views chart
        const viewsChartCanvas = document.getElementById('viewsChart');
        if (viewsChartCanvas) {
            const viewsChart = new Chart(viewsChartCanvas, {
                type: 'bar',
                data: {
                    labels: this.getLast7Days(),
                    datasets: [{
                        label: 'Lượt xem',
                        data: this.getRandomData(7, 100, 500),
                        backgroundColor: '#3b82f6',
                        borderColor: '#2563eb',
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    plugins: {
                        legend: {
                            position: 'top',
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
        
        // Genres chart
        const genresChartCanvas = document.getElementById('genresChart');
        if (genresChartCanvas) {
            const genresChart = new Chart(genresChartCanvas, {
                type: 'doughnut',
                data: {
                    labels: ['Hành động', 'Tình cảm', 'Hài hước', 'Kinh dị', 'Phiêu lưu'],
                    datasets: [{
                        data: this.getRandomData(5, 10, 30),
                        backgroundColor: [
                            '#6366f1', '#8b5cf6', '#ec4899', '#f43f5e', '#f59e0b'
                        ],
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    plugins: {
                        legend: {
                            position: 'right',
                        }
                    }
                }
            });
        }
    }
    
    /**
     * Initialize DataTables if available
     */
    initializeDataTables() {
        // Check if DataTable is available
        if (typeof $.fn.DataTable === 'undefined') return;
        
        // Initialize DataTables
        $('.datatable').DataTable({
            language: {
                search: "Tìm kiếm:",
                lengthMenu: "Hiển thị _MENU_ mục",
                info: "Hiển thị _START_ đến _END_ của _TOTAL_ mục",
                infoEmpty: "Hiển thị 0 đến 0 của 0 mục",
                infoFiltered: "(lọc từ _MAX_ mục)",
                paginate: {
                    first: "Đầu",
                    last: "Cuối",
                    next: "Tiếp",
                    previous: "Trước"
                }
            },
            responsive: true,
            pageLength: 10
        });
    }
    
    /**
     * Initialize form validation
     */
    initializeFormValidation() {
        const forms = document.querySelectorAll('.needs-validation');
        
        Array.from(forms).forEach(form => {
            form.addEventListener('submit', event => {
                if (!form.checkValidity()) {
                    event.preventDefault();
                    event.stopPropagation();
                }
                
                form.classList.add('was-validated');
            }, false);
        });
    }
    
    /**
     * Get array of last 7 days
     * @returns {Array} Array of date strings
     */
    getLast7Days() {
        const result = [];
        for (let i = 6; i >= 0; i--) {
            const d = new Date();
            d.setDate(d.getDate() - i);
            result.push(d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' }));
        }
        return result;
    }
    
    /**
     * Generate random data for charts
     * param {number} count - Number of data points
     * param {number} min - Minimum value
     * param {number} max - Maximum value
     * @returns {Array} Array of random numbers
     */
    getRandomData(count, min, max) {
        return Array.from({ length: count }, () => Math.floor(Math.random() * (max - min + 1)) + min);
    }
}

// Initialize admin layout
const adminLayout = new AdminLayout();

// Export the AdminLayout class and instance
window.AdminLayout = AdminLayout;
window.adminLayout = adminLayout;
