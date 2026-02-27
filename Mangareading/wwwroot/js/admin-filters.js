/**
 * Admin Filters JavaScript
 * Handles filtering functionality for admin tables
 */

document.addEventListener('DOMContentLoaded', function() {
    // Initialize user filters
    initUserFilters();
    
    // Initialize manga filters
    initMangaFilters();
});

/**
 * Initialize user filter functionality
 */
function initUserFilters() {
    const userFilterItems = document.querySelectorAll('.user-management-container .dropdown-menu .dropdown-item');
    if (!userFilterItems.length) return;
    
    userFilterItems.forEach(item => {
        item.addEventListener('click', function(e) {
            e.preventDefault();
            
            // Update active state
            userFilterItems.forEach(i => i.classList.remove('active'));
            this.classList.add('active');
            
            // Update dropdown button text
            const filterText = this.textContent.trim();
            const filterDropdown = document.querySelector('.user-management-container #filterDropdown');
            if (filterDropdown) {
                filterDropdown.innerHTML = `<i class="fas fa-filter me-1"></i> ${filterText}`;
            }
            
            // Get filter type
            const filterType = this.getAttribute('data-filter');
            
            // Apply filter
            filterUsers(filterType);
        });
    });
}

/**
 * Filter users based on filter type
 * param {string} filterType - The type of filter to apply
 */
function filterUsers(filterType) {
    const tableRows = document.querySelectorAll('.user-management-container .admin-table tbody tr');
    if (!tableRows.length) return;
    
    const today = new Date();
    const thirtyDaysAgo = new Date(today);
    thirtyDaysAgo.setDate(today.getDate() - 30);
    
    tableRows.forEach(row => {
        // Default to showing the row
        let showRow = true;
        
        // Get user status
        const statusCell = row.querySelector('td:nth-child(5)');
        const isActive = statusCell && statusCell.textContent.trim().includes('Kích hoạt');
        
        // Get registration date
        const dateCell = row.querySelector('td:nth-child(4)');
        let registrationDate = null;
        if (dateCell) {
            const dateParts = dateCell.textContent.trim().split('/');
            if (dateParts.length === 3) {
                // Format is dd/MM/yyyy
                registrationDate = new Date(dateParts[2], dateParts[1] - 1, dateParts[0]);
            }
        }
        
        // Apply filter based on type
        switch(filterType) {
            case 'active':
                showRow = isActive;
                break;
            case 'inactive':
                showRow = !isActive;
                break;
            case 'recent':
                showRow = registrationDate && registrationDate >= thirtyDaysAgo;
                break;
            case 'all':
            default:
                showRow = true;
                break;
        }
        
        // Show or hide row
        row.style.display = showRow ? '' : 'none';
    });
    
    // Show or hide "no results" message if needed
    updateNoResultsMessage('.user-management-container');
}

/**
 * Initialize manga filter functionality
 */
function initMangaFilters() {
    const mangaFilterItems = document.querySelectorAll('.manga-management-container .dropdown-menu .dropdown-item');
    if (!mangaFilterItems.length) return;
    
    mangaFilterItems.forEach(item => {
        item.addEventListener('click', function(e) {
            e.preventDefault();
            
            // Update active state
            mangaFilterItems.forEach(i => i.classList.remove('active'));
            this.classList.add('active');
            
            // Update dropdown button text
            const filterText = this.textContent.trim();
            const filterDropdown = document.querySelector('.manga-management-container #filterDropdown');
            if (filterDropdown) {
                filterDropdown.innerHTML = `<i class="fas fa-filter me-1"></i> ${filterText}`;
            }
            
            // Get filter type
            const filterType = this.getAttribute('data-filter');
            
            // Apply filter
            filterMangas(filterType);
        });
    });
}

/**
 * Filter mangas based on filter type
 * param {string} filterType - The type of filter to apply
 */
function filterMangas(filterType) {
    const tableRows = document.querySelectorAll('.manga-management-container .admin-table tbody tr');
    if (!tableRows.length) return;
    
    // If sorting by views, we need to collect all rows, sort them, and reinsert
    if (filterType === 'most-viewed') {
        sortMangasByViews(tableRows);
        return;
    }
    
    tableRows.forEach(row => {
        // Default to showing the row
        let showRow = true;
        
        // For status filtering, we need to check the status in the title cell
        // Since status isn't directly visible in the table, we'll use a data attribute
        // that we'll add to the rows
        const status = row.getAttribute('data-status') || '';
        
        // Apply filter based on type
        switch(filterType) {
            case 'completed':
                showRow = status.toLowerCase() === 'completed';
                break;
            case 'ongoing':
                showRow = status.toLowerCase() === 'ongoing';
                break;
            case 'all':
            default:
                showRow = true;
                break;
        }
        
        // Show or hide row
        row.style.display = showRow ? '' : 'none';
    });
    
    // Show or hide "no results" message if needed
    updateNoResultsMessage('.manga-management-container');
}

/**
 * Sort manga rows by view count
 * param {NodeList} rows - The table rows to sort
 */
function sortMangasByViews(rows) {
    const tbody = document.querySelector('.manga-management-container .admin-table tbody');
    if (!tbody || !rows.length) return;
    
    // Convert NodeList to Array for sorting
    const rowsArray = Array.from(rows);
    
    // Sort rows by view count (descending)
    rowsArray.sort((a, b) => {
        const viewsA = parseInt(a.querySelector('td:nth-child(5)').textContent.replace(/,/g, '')) || 0;
        const viewsB = parseInt(b.querySelector('td:nth-child(5)').textContent.replace(/,/g, '')) || 0;
        return viewsB - viewsA;
    });
    
    // Remove existing rows
    rows.forEach(row => row.remove());
    
    // Add sorted rows back to the table
    rowsArray.forEach(row => {
        tbody.appendChild(row);
        row.style.display = ''; // Make sure all rows are visible
    });
    
    // Show or hide "no results" message if needed
    updateNoResultsMessage('.manga-management-container');
}

/**
 * Update the "no results" message visibility
 * param {string} containerSelector - The selector for the container
 */
function updateNoResultsMessage(containerSelector) {
    const container = document.querySelector(containerSelector);
    if (!container) return;
    
    const visibleRows = container.querySelectorAll('.admin-table tbody tr[style=""]');
    const noResultsDiv = container.querySelector('.no-results');
    
    if (noResultsDiv) {
        noResultsDiv.style.display = visibleRows.length === 0 ? 'block' : 'none';
    }
}
