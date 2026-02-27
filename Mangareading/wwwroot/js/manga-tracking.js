/**
 * Manga Tracking
 * Handles tracking manga reading progress, views, and other statistics
 */

// Check if MangaTracking is already defined to prevent duplicate declarations
window.MangaTracking = window.MangaTracking || (function() {
    // Private variables
    let isAuthenticated = false;

    /**
     * Initialize the tracking module
     */
    function init(authenticated = false) {
        isAuthenticated = authenticated;
        console.log('Manga tracking initialized, authenticated:', isAuthenticated);

        // Set up online/offline event listeners
        window.addEventListener('online', function() {
            console.log('Back online, attempting to sync reading progress');
            syncReadingProgress();
        });

        // Try to sync any pending progress updates
        if (navigator.onLine && isAuthenticated) {
            setTimeout(function() {
                syncReadingProgress();
            }, 5000); // Wait 5 seconds after initialization to sync
        }
    }

    /**
     * Sync reading progress from queue
     */
    function syncReadingProgress() {
        if (!isAuthenticated || !navigator.onLine) {
            return;
        }

        try {
            const syncQueue = JSON.parse(localStorage.getItem('reading_progress_queue') || '[]');
            if (syncQueue.length === 0) {
                 // Ensure icon is idle if queue is empty
                 if (window.readingLayout && typeof window.readingLayout.updateSyncStatusIcon === 'function') {
                    window.readingLayout.updateSyncStatusIcon('idle');
                 }
                return;
            }

            console.log(`Attempting to sync ${syncQueue.length} reading progress items`);
            
            // Indicate syncing started
            if (window.readingLayout && typeof window.readingLayout.updateSyncStatusIcon === 'function') {
                window.readingLayout.updateSyncStatusIcon('syncing');
            }

            // Process each item in the queue
            // Note: This sends multiple requests. Consider batching if performance is an issue.
            syncQueue.forEach(function(item) {
                sendProgressToServer(
                    item.mangaId,
                    item.chapterId,
                    item.currentPage,
                    item.totalPages,
                    item.progressPercent
                );
            });
        } catch (e) {
            console.error('Error syncing reading progress:', e);
            // Update UI to show error status on sync error
            if (window.readingLayout && typeof window.readingLayout.updateSyncStatusIcon === 'function') {
                window.readingLayout.updateSyncStatusIcon('error');
            }
        }
    }

    /**
     * Record a view for a manga and chapter
     */
    function recordView(mangaId, chapterId) {
        if (!mangaId || !chapterId) {
            console.error('Invalid manga or chapter ID');
            return;
        }

        console.log('Recording view for manga:', mangaId, 'chapter:', chapterId);

        $.ajax({
            url: `/api/MangaStatistics/view/${mangaId}/${chapterId}`,
            type: 'POST',
            success: function(response) {
                console.log('View recorded successfully:', response);
            },
            error: function(xhr, status, error) {
                console.error('Error recording view:', error);
                console.error('Details:', xhr.responseText);

                // Try fallback API if main one fails
                fallbackRecordView(mangaId, chapterId);
            }
        });
    }

    /**
     * Fallback method to record a view
     */
    function fallbackRecordView(mangaId, chapterId) {
        console.log('Trying fallback API...');
        $.ajax({
            url: `/api/MangaTracking/view/${mangaId}/${chapterId}`,
            type: 'POST',
            success: function(response) {
                console.log('View recorded successfully (fallback):', response);
            },
            error: function(xhr, status, error) {
                console.error('Error recording view (fallback):', error);
            }
        });
    }

    /**
     * Update reading progress for a manga
     */
    function updateReadingProgress(mangaId, chapterId, pageNumber, totalPages) {
        if (!isAuthenticated) {
            console.log('User not authenticated, skipping progress update');
            return;
        }

        const progressPercent = Math.round((pageNumber / totalPages) * 100);

        // Save progress to localStorage as a fallback
        saveProgressToLocalStorage(mangaId, chapterId, pageNumber, totalPages, progressPercent);

        // Try to send to server if available
        if (navigator.onLine) {
            sendProgressToServer(mangaId, chapterId, pageNumber, totalPages, progressPercent);
        }
    }

    /**
     * Save reading progress to localStorage
     */
    function saveProgressToLocalStorage(mangaId, chapterId, pageNumber, totalPages, progressPercent) {
        try {
            const key = `reading_progress_${mangaId}_${chapterId}`;
            const data = {
                mangaId: mangaId,
                chapterId: chapterId,
                currentPage: pageNumber,
                totalPages: totalPages,
                progressPercent: progressPercent,
                timestamp: new Date().toISOString()
            };

            localStorage.setItem(key, JSON.stringify(data));
            console.log('Progress saved to localStorage');

            // Also save to a queue for later sync
            const syncQueue = JSON.parse(localStorage.getItem('reading_progress_queue') || '[]');
            syncQueue.push(data);
            localStorage.setItem('reading_progress_queue', JSON.stringify(syncQueue));
        } catch (e) {
            console.error('Error saving progress to localStorage:', e);
        }
    }

    /**
     * Send reading progress to server
     */
    function sendProgressToServer(mangaId, chapterId, pageNumber, totalPages, progressPercent) {
        // If we've already detected that the server doesn't support progress tracking,
        // don't keep trying to send requests that will fail
        if (localStorage.getItem('server_progress_unavailable') === 'true') {
            // Just log a debug message instead of an error
            console.debug('Server progress tracking unavailable, using local storage only');
            return;
        }

        // Update UI to show syncing status
        if (window.readingLayout && typeof window.readingLayout.updateSyncStatusIcon === 'function') {
            window.readingLayout.updateSyncStatusIcon('syncing');
        }

        const progressData = JSON.stringify({
            mangaId: mangaId,
            chapterId: chapterId,
            currentPage: pageNumber,
            totalPages: totalPages,
            progressPercent: progressPercent
        });

        // Track if this is the first attempt to sync for this session
        const isFirstAttempt = localStorage.getItem('progress_sync_attempted') !== 'true';

        // First try the MangaTracking endpoint
        $.ajax({
            url: `/api/MangaTracking/progress`,
            type: 'POST',
            data: progressData,
            contentType: 'application/json',
            success: function(response) {
                console.log('Progress updated successfully:', response);
                removeFromSyncQueue(mangaId, chapterId);
                showSyncSuccess();
                // Update UI to show synced status
                if (window.readingLayout && typeof window.readingLayout.updateSyncStatusIcon === 'function') {
                    window.readingLayout.updateSyncStatusIcon('synced');
                }
            },
            error: function(xhr, status, error) {
                // Only log detailed errors on first attempt
                if (isFirstAttempt) {
                    console.debug('First endpoint unavailable, trying fallback');
                }

                // Try fallback endpoint
                $.ajax({
                    url: `/api/Reading/save-progress`,
                    type: 'POST',
                    data: {
                        chapterId: chapterId,
                        mangaId: mangaId,
                        pageNumber: pageNumber
                    },
                    success: function(response) {
                        console.log('Progress updated successfully (fallback):', response);
                        removeFromSyncQueue(mangaId, chapterId);
                        showSyncSuccess();
                        // Update UI to show synced status
                        if (window.readingLayout && typeof window.readingLayout.updateSyncStatusIcon === 'function') {
                            window.readingLayout.updateSyncStatusIcon('synced');
                        }
                    },
                    error: function(xhr2, status2, error2) {
                        // Only log detailed errors on first attempt
                        if (isFirstAttempt) {
                            console.debug('Second endpoint unavailable, trying final fallback');
                        }

                        // Try another fallback endpoint
                        $.ajax({
                            url: `/api/MangaStatistics/progress`,
                            type: 'POST',
                            data: progressData,
                            contentType: 'application/json',
                            success: function(response) {
                                console.log('Progress updated successfully (second fallback):', response);
                                removeFromSyncQueue(mangaId, chapterId);
                                showSyncSuccess();
                                // Update UI to show synced status
                                if (window.readingLayout && typeof window.readingLayout.updateSyncStatusIcon === 'function') {
                                    window.readingLayout.updateSyncStatusIcon('synced');
                                }
                            },
                            error: function(xhr3, status3, error3) {
                                // Only log detailed errors on first attempt
                                if (isFirstAttempt) {
                                    console.log('All progress update endpoints failed, using local storage only');
                                    localStorage.setItem('server_progress_unavailable', 'true');
                                    localStorage.setItem('progress_sync_attempted', 'true');

                                    // Show a message to the user only on first attempt
                                    showToast('info', 'Tiến trình đọc đang được lưu cục bộ', 5000);
                                }
                                
                                // Update UI to show error status
                                if (window.readingLayout && typeof window.readingLayout.updateSyncStatusIcon === 'function') {
                                    window.readingLayout.updateSyncStatusIcon('error');
                                }
                            }
                        });
                    }
                });
            }
        });
    }

    /**
     * Show a success indicator for sync
     */
    function showSyncSuccess() {
        // Set flag to indicate successful sync
        localStorage.setItem('server_progress_unavailable', 'false');
        localStorage.setItem('progress_sync_attempted', 'true');

        // Only show toast on first successful sync
        if (localStorage.getItem('progress_sync_success_shown') !== 'true') {
            showToast('success', 'Tiến trình đọc đã được đồng bộ', 3000);
            localStorage.setItem('progress_sync_success_shown', 'true');
        }
    }

    /**
     * Remove an item from the sync queue
     */
    function removeFromSyncQueue(mangaId, chapterId) {
        try {
            const syncQueue = JSON.parse(localStorage.getItem('reading_progress_queue') || '[]');
            const newQueue = syncQueue.filter(item => !(item.mangaId == mangaId && item.chapterId == chapterId));
            localStorage.setItem('reading_progress_queue', JSON.stringify(newQueue));
        } catch (e) {
            console.error('Error removing from sync queue:', e);
        }
    }

    /**
     * Add manga to user's reading list
     */
    function addToReadingList(mangaId) {
        if (!isAuthenticated) {
            showLoginPrompt();
            return;
        }

        $.ajax({
            url: `/api/MangaTracking/reading-list/add/${mangaId}`,
            type: 'POST',
            success: function(response) {
                console.log('Added to reading list:', response);
                showToast('success', 'Đã thêm vào danh sách đọc');
            },
            error: function(xhr, status, error) {
                console.error('Error adding to reading list:', error);
                showToast('error', 'Không thể thêm vào danh sách đọc');
            }
        });
    }

    /**
     * Remove manga from user's reading list
     */
    function removeFromReadingList(mangaId) {
        if (!isAuthenticated) {
            return;
        }

        $.ajax({
            url: `/api/MangaTracking/reading-list/remove/${mangaId}`,
            type: 'POST',
            success: function(response) {
                console.log('Removed from reading list:', response);
                showToast('success', 'Đã xóa khỏi danh sách đọc');
            },
            error: function(xhr, status, error) {
                console.error('Error removing from reading list:', error);
                showToast('error', 'Không thể xóa khỏi danh sách đọc');
            }
        });
    }

    /**
     * Show login prompt for unauthenticated users
     */
    function showLoginPrompt() {
        showToast('info', 'Vui lòng đăng nhập để sử dụng tính năng này', 5000);
    }

    /**
     * Show a toast notification
     */


    // Public API
    return {
        init: init,
        recordView: recordView,
        updateReadingProgress: updateReadingProgress,
        addToReadingList: addToReadingList,
        removeFromReadingList: removeFromReadingList,
        syncReadingProgress: syncReadingProgress,
        getReadingProgress: function(mangaId, chapterId) {
            try {
                const key = `reading_progress_${mangaId}_${chapterId}`;
                const data = JSON.parse(localStorage.getItem(key) || 'null');
                return data;
            } catch (e) {
                console.error('Error getting reading progress:', e);
                return null;
            }
        }
    };
})();
