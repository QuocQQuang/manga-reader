/**
 * Manga-specific functionality for the Manga Reader application
 */

document.addEventListener('DOMContentLoaded', function() {
    // Initialize chapter filter
    initChapterFilter();
    
    // Initialize favorite button
    initFavoriteButton();
    
    // Initialize read buttons
    initReadButtons();

    // Khởi chạy xử lý MangaDex CDN
    setupMangaDexCdnHandler();
});

/**
 * Initialize chapter filter functionality
 */
function initChapterFilter() {
    const chapterFilter = document.getElementById('chapterFilter');
    const chapterTableBody = document.querySelector('.chapter-table tbody'); // Get table body
    const noResultsDiv = document.getElementById('noChapterResults');

    if (chapterFilter && chapterTableBody) {
        chapterFilter.addEventListener('input', function() {
            const filter = this.value.toLowerCase().trim();
            let visibleCount = 0;

            chapterTableBody.querySelectorAll('tr.chapter-item').forEach(function(item) {
                // Find the cells containing chapter number and title
                // Adjust indices if table structure changes (0-based index)
                const chapterNumberCell = item.cells[0]; 
                const chapterTitleCell = item.cells[1]; 
                
                let textToSearch = '';
                if (chapterNumberCell) {
                    textToSearch += chapterNumberCell.textContent.toLowerCase().trim();
                }
                if (chapterTitleCell) {
                    textToSearch += ' ' + chapterTitleCell.textContent.toLowerCase().trim();
                }
                
                if (textToSearch.includes(filter)) {
                    item.style.display = '';
                    visibleCount++;
                } else {
                    item.style.display = 'none';
                }
            });

            // Show/hide 'no results' message
            if (noResultsDiv) {
                 noResultsDiv.style.display = visibleCount === 0 ? 'block' : 'none';
            }
        });
    }
}

/**
 * Initialize favorite button functionality
 */
function initFavoriteButton() {
    const favoriteBtn = document.querySelector('.favorite-btn');
    if (favoriteBtn) {
        favoriteBtn.addEventListener('click', function(e) {
            e.preventDefault();
            
            // If user is not logged in, redirect to login
            if (!isUserLoggedIn()) {
                window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
                return;
            }
            
            const mangaId = this.getAttribute('data-manga-id');
            const isFavorite = this.classList.contains('active');
            
            // Optimistic UI update
            this.classList.toggle('active');
            this.querySelector('i').classList.toggle('fa-heart');
            this.querySelector('i').classList.toggle('fa-heart-broken');
            this.querySelector('span').textContent = isFavorite ? 'Thêm vào Tủ Truyện' : 'Xóa khỏi Tủ Truyện';
            
            // Save favorite status to server using correct API endpoint
            $.ajax({
                url: `/api/MangaStatistics/favorite/${mangaId}`,
                method: 'POST',
                success: function(response) {
                    showToast(isFavorite ? 'Đã xóa khỏi danh sách yêu thích' : 'Đã thêm vào danh sách yêu thích', 'success');
                },
                error: function(xhr, status, error) {
                    console.error('Error:', error);
                    // Revert UI change on error
                    favoriteBtn.classList.toggle('active');
                    favoriteBtn.querySelector('i').classList.toggle('fa-heart');
                    favoriteBtn.querySelector('i').classList.toggle('fa-heart-broken');
                    favoriteBtn.querySelector('span').textContent = !isFavorite ? 'Thêm vào Tủ Truyện' : 'Xóa khỏi Tủ Truyện';
                    showToast('Có lỗi xảy ra khi cập nhật tủ truyện', 'danger');
                }
            });
        });
    }
}

/**
 * Initialize read buttons functionality
 */
function initReadButtons() {
    const readFirstBtn = document.querySelector('.read-first-btn');
    const readLastBtn = document.querySelector('.read-last-btn');
    const readContinueBtn = document.querySelector('.read-continue-btn');
    
    if (readFirstBtn) {
        readFirstBtn.addEventListener('click', function() {
            const chapterId = this.getAttribute('data-chapter-id');
            if (chapterId) {
                window.location.href = `/Chapter/Read/${chapterId}`;
            } else {
                showToast('Không tìm thấy chapter đầu tiên', 'warning');
            }
        });
    }
    
    if (readLastBtn) {
        readLastBtn.addEventListener('click', function() {
            const chapterId = this.getAttribute('data-chapter-id');
            if (chapterId) {
                window.location.href = `/Chapter/Read/${chapterId}`;
            } else {
                showToast('Không tìm thấy chapter mới nhất', 'warning');
            }
        });
    }
    
    if (readContinueBtn) {
        readContinueBtn.addEventListener('click', function() {
            const chapterId = this.getAttribute('data-chapter-id');
            if (chapterId) {
                window.location.href = `/Chapter/Read/${chapterId}`;
            } else {
                // If no continue chapter, redirect to first chapter
                const firstChapterId = readFirstBtn.getAttribute('data-chapter-id');
                if (firstChapterId) {
                    window.location.href = `/Chapter/Read/${firstChapterId}`;
                } else {
                    showToast('Không tìm thấy chapter để đọc', 'warning');
                }
            }
        });
    }
}

/**
 * Get relative time string for a timestamp
 * param {Date|string|number} date - Date object, ISO string, or timestamp
 * @returns {string} - Relative time string
 */
function getRelativeTime(date) {
    if (!(date instanceof Date)) {
        date = new Date(date);
    }
    
    const now = new Date();
    const diffMs = now - date;
    const diffSec = Math.floor(diffMs / 1000);
    const diffMin = Math.floor(diffSec / 60);
    const diffHour = Math.floor(diffMin / 60);
    const diffDay = Math.floor(diffHour / 24);
    const diffMonth = Math.floor(diffDay / 30);
    const diffYear = Math.floor(diffDay / 365);
    
    if (diffSec < 60) {
        return 'vừa xong';
    } else if (diffMin < 60) {
        return `${diffMin} phút trước`;
    } else if (diffHour < 24) {
        return `${diffHour} giờ trước`;
    } else if (diffDay < 30) {
        return `${diffDay} ngày trước`;
    } else if (diffMonth < 12) {
        return `${diffMonth} tháng trước`;
    } else {
        return `${diffYear} năm trước`;
    }
}

/**
 * Update all relative time elements on the page
 */
function updateRelativeTimes() {
    document.querySelectorAll('[data-time]').forEach(element => {
        const timestamp = element.getAttribute('data-time');
        element.textContent = getRelativeTime(timestamp);
    });
}

// Update relative times every minute
setInterval(updateRelativeTimes, 60000);

// Export functions for global use
window.getRelativeTime = getRelativeTime;

/**
 * Thêm cơ chế xử lý đặc biệt cho MangaDex CDN images
 */
function setupMangaDexCdnHandler() {
    // Danh sách các domain CDN của MangaDex để kiểm tra
    const mangadexCdnDomains = [
        'mangadex.org',
        'uploads.mangadex.org',
        'mangadex.network',
        'mangadex.com',
        'mangadex.net',
        'cmdxd',
        'mngsrv'
    ];
    
    // Kiểm tra URL có phải từ MangaDex CDN không
    function isMangaDexCdn(url) {
        if (!url) return false;
        try {
            const urlObj = new URL(url, window.location.origin);
            return mangadexCdnDomains.some(domain => urlObj.hostname.includes(domain));
        } catch {
            return false;
        }
    }
    
    // Giám sát các request hình ảnh trên trang
    function monitorImageRequests() {
        // Tìm tất cả hình ảnh hiện tại và thêm xử lý đặc biệt
        document.querySelectorAll('img').forEach(setupImageElement);
        
        // Sử dụng MutationObserver để phát hiện hình ảnh mới được thêm vào DOM
        const observer = new MutationObserver(mutations => {
            mutations.forEach(mutation => {
                mutation.addedNodes.forEach(node => {
                    // Xử lý nếu node là hình ảnh
                    if (node.tagName === 'IMG') {
                        setupImageElement(node);
                    }
                    
                    // Tìm các hình ảnh trong node con
                    if (node.querySelectorAll) {
                        node.querySelectorAll('img').forEach(setupImageElement);
                    }
                });
            });
        });
        
        // Bắt đầu quan sát thay đổi trong DOM
        observer.observe(document.body, { childList: true, subtree: true });
    }
    
    // Thiết lập xử lý đặc biệt cho phần tử hình ảnh
    function setupImageElement(img) {
        // Bỏ qua nếu đã xử lý
        if (img.dataset.mdxHandled) return;
        img.dataset.mdxHandled = 'true';
        
        // Lưu URL gốc nếu có
        const originalSrc = img.getAttribute('data-src') || img.getAttribute('src');
        if (!originalSrc) return;
        
        // Chỉ áp dụng xử lý đặc biệt cho hình ảnh từ MangaDex CDN
        if (isMangaDexCdn(originalSrc)) {
            // Lưu URL gốc để thử lại nếu cần
            img.setAttribute('data-original-mdx-src', originalSrc);
            
            // Thêm crossorigin attribute
            img.setAttribute('crossorigin', 'anonymous');
            
            // Xử lý khi hình ảnh load thất bại
            img.addEventListener('error', function(e) {
                handleMangaDexImageError(this);
            });
        }
    }
    
    // Xử lý lỗi khi tải hình ảnh từ MangaDex CDN
    function handleMangaDexImageError(img) {
        // Không xử lý nếu hình ảnh đã được thay thế bằng placeholder
        if (img.classList.contains('md-fallback-applied')) return;
        
        // Lấy URL gốc
        const originalSrc = img.getAttribute('data-original-mdx-src');
        if (!originalSrc) return;
        
        // Đánh dấu để tránh xử lý lại nhiều lần
        img.classList.add('md-fallback-applied');
        
        // Lưu số lần thử lại
        const retryCount = parseInt(img.dataset.retryCount || '0') + 1;
        img.dataset.retryCount = retryCount.toString();
        
        // Giới hạn số lần thử lại
        if (retryCount <= 3) {
            console.log(`Đang thử lại tải hình ảnh MangaDex lần ${retryCount}: ${originalSrc}`);
            
            // Thời gian chờ tăng dần theo số lần thử lại
            setTimeout(() => {
                // Xóa đánh dấu lỗi để có thể thử lại
                img.classList.remove('md-fallback-applied');
                
                // Thay đổi cách xử lý CORS trong mỗi lần thử
                if (retryCount % 2 === 0) {
                    img.removeAttribute('crossorigin');
                } else {
                    img.setAttribute('crossorigin', 'anonymous');
                }
                
                // Thêm cache-busting parameter để buộc trình duyệt tải lại
                const cacheBuster = `?retry=${retryCount}&t=${Date.now()}`;
                img.src = originalSrc.includes('?') 
                    ? `${originalSrc}&retry=${retryCount}` 
                    : `${originalSrc}${cacheBuster}`;
                
            }, retryCount * 1000); // Tăng thời gian chờ theo số lần thử
        } else {
            // Sau khi thử lại đủ số lần, hiển thị placeholder
            console.warn(`Không thể tải hình ảnh sau ${retryCount} lần thử: ${originalSrc}`);
            
            // Áp dụng hình ảnh placeholder
            applyImagePlaceholder(img, originalSrc);
        }
    }
    
    // Áp dụng placeholder cho hình ảnh lỗi
    function applyImagePlaceholder(img, originalSrc) {
        // Lưu kích thước gốc của hình ảnh nếu có
        const width = img.width || img.offsetWidth || 200;
        const height = img.height || img.offsetHeight || 300;
        
        // Thêm class để style có thể nhận diện
        img.classList.add('manga-image-error');
        
        // Đặt ảnh placeholder
        img.src = '/images/no-cover.png'; // Sử dụng ảnh placeholder mặc định
        
        // Giữ nguyên kích thước
        if (width && height) {
            img.style.width = `${width}px`;
            img.style.height = `${height}px`;
            img.style.objectFit = 'contain';
        }
        
        // Thêm tooltip hiển thị lỗi
        img.title = `Không thể tải hình ảnh. URL: ${originalSrc}`;
    }
    
    // Khởi chạy giám sát khi DOM đã sẵn sàng
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', monitorImageRequests);
    } else {
        monitorImageRequests();
    }
}