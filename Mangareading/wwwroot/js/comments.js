// Utility function to resolve JSON references
function resolveReferences(obj) {
    // Create a cache to resolve references
    const objectCache = {};

    // First pass: build the cache
    function buildCache(obj) {
        if (!obj || typeof obj !== 'object') return;

        if (obj.$id) {
            objectCache[obj.$id] = obj;
        }

        // Process arrays
        if (Array.isArray(obj)) {
            obj.forEach(item => buildCache(item));
        } else {
            // Process object properties
            for (const key in obj) {
                if (obj.hasOwnProperty(key) && key !== '$id' && key !== '$ref') {
                    buildCache(obj[key]);
                }
            }
        }
    }

    // Second pass: resolve references
    function resolveRefs(obj) {
        if (!obj || typeof obj !== 'object') return obj;

        // If it's a reference, resolve it
        if (obj.$ref) {
            const resolved = objectCache[obj.$ref];
            if (resolved) {
                // Make a deep copy to avoid circular references
                return JSON.parse(JSON.stringify(resolved));
            }
            return obj;
        }

        // Process arrays
        if (Array.isArray(obj)) {
            return obj.map(item => resolveRefs(item));
        } else {
            // Process object properties
            const result = {};
            for (const key in obj) {
                if (obj.hasOwnProperty(key) && key !== '$id') {
                    result[key] = resolveRefs(obj[key]);
                }
            }
            return result;
        }
    }

    // Build the cache first
    buildCache(obj);

    // Then resolve references
    const resolved = resolveRefs(obj);

    // Additional pass to handle any nested references that might have been missed
    function deepResolve(obj) {
        if (!obj || typeof obj !== 'object') return obj;

        // Process arrays
        if (Array.isArray(obj)) {
            return obj.map(item => deepResolve(item));
        }

        // Process object properties
        const result = {};
        for (const key in obj) {
            if (obj.hasOwnProperty(key)) {
                const value = obj[key];
                if (value && value.$ref && objectCache[value.$ref]) {
                    // Replace reference with actual object
                    result[key] = deepResolve(objectCache[value.$ref]);
                } else {
                    result[key] = deepResolve(value);
                }
            }
        }
        return result;
    }

    // Do a final deep resolve pass
    return deepResolve(resolved);
}

// Comments handling for manga and chapters
// Initialize event handlers when document is ready
function initComments() {
    console.log('Initializing comment system...');

    // Debug DOM elements
    console.log('DOM elements found:', {
        mangaCommentsContainer: $('#mangaCommentsContainer').length > 0,
        chapterCommentsContainer: $('#chapterCommentsContainer').length > 0,
        submitMangaComment: $('#submitMangaComment').length > 0,
        submitChapterComment: $('#submitChapterComment').length > 0,
        loadMoreMangaComments: $('#loadMoreMangaComments').length > 0,
        loadMoreChapterComments: $('#loadMoreChapterComments').length > 0,
        chapterId: $('#chapterId').val(),
        mangaId: $('#mangaId').val()
    });

    // Load manga comments on page load if we're on the manga details page
    if ($('#mangaCommentsContainer').length > 0) {
        console.log('Loading manga comments...');
        loadMangaComments(1);

        // Load chapter comments when tab is clicked
        $('#chapter-comments-tab').off('shown.bs.tab').on('shown.bs.tab', function() {
            console.log('Chapter comments tab clicked');
            loadChapterComments(1);
        });

        // Submit manga comment
        $('#submitMangaComment').off('click').on('click', function() {
            const mangaId = $(this).data('manga-id');
            const content = $('#mangaCommentText').val().trim();

            console.log('Submit manga comment clicked:', { mangaId, content });

            if (content === '') {
                showToast('Vui lòng nhập nội dung bình luận', 'warning');
                return;
            }

            submitComment(mangaId, null, content);
        });

        // Load more manga comments
        $('#loadMoreMangaComments button').off('click').on('click', function() {
            const page = $(this).data('page');
            const mangaId = $(this).data('manga-id');

            console.log('Load more manga comments clicked:', { page, mangaId });
            loadMangaComments(page);
            $(this).data('page', page + 1);
        });

        // Load more chapter comments
        $('#loadMoreChapterComments button').off('click').on('click', function() {
            const page = $(this).data('page');
            const mangaId = $(this).data('manga-id');

            console.log('Load more chapter comments clicked:', { page, mangaId });
            loadChapterComments(page);
            $(this).data('page', page + 1);
        });
    }

    // Load chapter comments if we're on the chapter read page
    if ($('#chapterCommentsContainer').length > 0) {
        console.log('Loading chapter comments for chapter page...');

        // Get chapter ID from global variable or DOM
        const chapterId = window.currentChapterId || $('#chapterId').val();
        const mangaId = window.currentMangaId || $('#mangaId').val();

        console.log('Chapter page detected with:', { chapterId, mangaId });

        // Force load chapter comments
        setTimeout(function() {
            loadChapterComments(1);
        }, 500);

        // Submit chapter comment (from chapter detail page)
        $('#submitChapterComment').off('click').on('click', function() {
            const mangaId = $(this).data('manga-id');
            const chapterId = $(this).data('chapter-id');
            const content = $('#chapterCommentText').val().trim();

            console.log('Submit chapter comment clicked:', { mangaId, chapterId, content });

            if (content === '') {
                showToast('Vui lòng nhập nội dung bình luận', 'warning');
                return;
            }

            submitComment(mangaId, chapterId, content);
        });

        // Load more chapter comments
        $('#loadMoreChapterComments button').off('click').on('click', function() {
            const page = $(this).data('page');
            console.log('Load more chapter comments clicked:', { page });
            loadChapterComments(page);
            $(this).data('page', page + 1);
        });
    }

    // Global event handlers (for dynamically created elements)
    // Handle reply form submission
    $(document).off('click', '.submit-reply-btn').on('click', '.submit-reply-btn', function() {
        const commentId = $(this).data('comment-id');
        const replyContent = $(`#replyText-${commentId}`).val().trim();

        if (replyContent === '') {
            showToast('Vui lòng nhập nội dung trả lời', 'warning');
            return;
        }

        submitReply(commentId, replyContent);
    });

    // Toggle reply form
    $(document).off('click', '.reply-btn').on('click', '.reply-btn', function() {
        const commentId = $(this).data('comment-id');
        $(`#replyForm-${commentId}`).toggleClass('d-none');
    });

    // Handle like/dislike for comments and replies
    $(document).off('click', '.reaction-btn').on('click', '.reaction-btn', function() {
        if (typeof isAuthenticated === 'undefined' || !isAuthenticated) {
            window.location.href = '/Account/Login';
            return;
        }

        const commentId = $(this).data('comment-id');
        const replyId = $(this).data('reply-id');
        const isLike = $(this).hasClass('like-btn');
        
        console.log('Reaction button clicked:', { 
            commentId: commentId, 
            replyId: replyId, 
            isLike: isLike, 
            buttonClass: this.className
        });
        
        // Determine which container to update - IMPORTANT: This selector must be SPECIFIC to avoid affecting other elements
        const container = replyId ? `#reply-${replyId}` : `#comment-${commentId}`;
        
        // Only affect THIS SPECIFIC button and its opposite in the SAME container
        const button = $(this);
        const oppositeButton = button.siblings(isLike ? '.dislike-btn' : '.like-btn');
        
        // Visual feedback before server response
        if (button.hasClass('active')) {
            // If already active, toggle it off
            button.removeClass('active');
        } else {
            // Toggle on this button, toggle off the opposite
            button.addClass('active');
            oppositeButton.removeClass('active');
        }

        // Make sure we're properly distinguishing between comment and reply reactions
        if (commentId) {
            submitReaction(commentId, null, isLike);
        } else if (replyId) {
            submitReaction(null, replyId, isLike);
        } else {
            console.error('Error: Neither commentId nor replyId found on button');
        }
    });

    console.log('Comment system initialized');
}

// Initialize when document is ready
$(document).ready(function() {
    initComments();
});

// Function to load manga comments
function loadMangaComments(page) {
    // Try to get mangaId from button, then from global variable
    let mangaId = $('#submitMangaComment').data('manga-id');

    // If mangaId is not available from the button, use the global variable
    if (!mangaId && typeof currentMangaId !== 'undefined') {
        mangaId = currentMangaId;
    }

    console.log('Loading manga comments for mangaId:', mangaId, 'page:', page);

    // Show loading indicator
    if (page === 1) {
        $('#mangaCommentsContainer').html(`
            <div class="text-center">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <p class="mt-2">Đang tải bình luận...</p>
            </div>
        `);
    }

    $.ajax({
        url: `/api/Comment/manga/${mangaId}?page=${page}&pageSize=10`,
        type: 'GET',
        success: function(comments) {
            console.log('Loaded manga comments:', comments);

            if (page === 1) {
                // Clear container for first page
                $('#mangaCommentsContainer').empty();
            }

            // Use the global resolveReferences function

            // Handle different response formats
            let commentsArray = comments;

            // Check if the response is an object with a $values property (common in .NET)
            if (!Array.isArray(comments) && comments && comments.$values) {
                console.log('Detected $values format, extracting array');

                // First, resolve any references in the entire response
                const resolvedComments = resolveReferences(comments);
                commentsArray = resolvedComments.$values || [];

                console.log('Resolved references in JSON response');
            }

            // Check if the response is still not an array
            if (!Array.isArray(commentsArray)) {
                console.error('Expected an array of comments, but got:', comments);
                $('#mangaCommentsContainer').html('<p class="text-danger text-center">Lỗi: Dữ liệu bình luận không hợp lệ</p>');
                return;
            }

            if (commentsArray.length === 0 && page === 1) {
                $('#mangaCommentsContainer').html('<p class="text-muted text-center">Chưa có bình luận nào. Hãy là người đầu tiên bình luận!</p>');
                $('#loadMoreMangaComments').addClass('d-none');
                return;
            }

            // Show or hide load more button
            if (commentsArray.length < 10) {
                $('#loadMoreMangaComments').addClass('d-none');
            } else {
                $('#loadMoreMangaComments').removeClass('d-none');
            }

            // Render comments
            commentsArray.forEach(comment => {
                try {
                    const commentHtml = renderComment(comment);
                    $('#mangaCommentsContainer').append(commentHtml);
                } catch (error) {
                    console.error('Error rendering comment:', error, comment);
                }
            });
        },
        error: function(xhr, status, error) {
            console.error('Error loading manga comments:', error);
            console.error('Status:', status);
            console.error('Response:', xhr.responseText);
            $('#mangaCommentsContainer').html('<p class="text-danger text-center">Không thể tải bình luận. Vui lòng thử lại sau.</p>');
        }
    });
}

// Function to load chapter comments
function loadChapterComments(page) {
    // Check if we're on a chapter page or manga details page
    let url;

    // Try to get chapterId from global variable or from DOM
    let chapterId = window.currentChapterId || $('#chapterId').val();
    if (chapterId && typeof chapterId === 'string') {
        chapterId = parseInt(chapterId);
    }

    // Try to get mangaId from button, then from global variable, then from DOM
    let mangaId = $('#submitChapterComment').data('manga-id') ||
                 $('#submitMangaComment').data('manga-id') ||
                 window.currentMangaId ||
                 $('#mangaId').val();

    if (mangaId && typeof mangaId === 'string') {
        mangaId = parseInt(mangaId);
    }

    console.log('loadChapterComments called with:', {
        page: page,
        chapterId: chapterId,
        mangaId: mangaId,
        currentChapterId: window.currentChapterId,
        currentMangaId: window.currentMangaId
    });

    // Determine which API endpoint to use
    if (chapterId) {
        // If we're on a chapter page, load comments for that specific chapter
        url = `/api/Comment/chapter/${chapterId}?page=${page}&pageSize=10`;
        console.log('Loading comments for specific chapter:', chapterId, 'page:', page);
    } else {
        // If we're on the manga details page, load all chapter comments for the manga
        url = `/api/Comment/manga/${mangaId}/chapters?page=${page}&pageSize=10`;
        console.log('Loading all chapter comments for manga:', mangaId, 'page:', page);
    }

    // Show loading indicator
    if (page === 1) {
        $('#chapterCommentsContainer').html(`
            <div class="text-center">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <p class="mt-2">Đang tải bình luận chapter...</p>
            </div>
        `);
    }

    $.ajax({
        url: url,
        type: 'GET',
        success: function(comments) {
            console.log('Loaded chapter comments:', comments);

            if (page === 1) {
                // Clear container for first page
                $('#chapterCommentsContainer').empty();
            }

            // Use the global resolveReferences function

            // Handle different response formats
            let commentsArray = comments;

            // Check if the response is an object with a $values property (common in .NET)
            if (!Array.isArray(comments) && comments && comments.$values) {
                console.log('Detected $values format, extracting array');

                // First, resolve any references in the entire response
                const resolvedComments = resolveReferences(comments);
                commentsArray = resolvedComments.$values || [];

                console.log('Resolved references in JSON response');
            }

            // Check if the response is still not an array
            if (!Array.isArray(commentsArray)) {
                console.error('Expected an array of comments, but got:', comments);
                $('#chapterCommentsContainer').html('<p class="text-danger text-center">Lỗi: Dữ liệu bình luận không hợp lệ</p>');
                return;
            }

            if (commentsArray.length === 0 && page === 1) {
                $('#chapterCommentsContainer').html('<p class="text-muted text-center">Chưa có bình luận nào cho chapter này.</p>');
                $('#loadMoreChapterComments').addClass('d-none');
                return;
            }

            // Show or hide load more button
            if (commentsArray.length < 10) {
                $('#loadMoreChapterComments').addClass('d-none');
            } else {
                $('#loadMoreChapterComments').removeClass('d-none');
            }

            // Render comments
            commentsArray.forEach(comment => {
                try {
                    const commentHtml = renderComment(comment, true);
                    $('#chapterCommentsContainer').append(commentHtml);
                } catch (error) {
                    console.error('Error rendering comment:', error, comment);
                }
            });
        },
        error: function(xhr, status, error) {
            console.error('Error loading chapter comments:', error);
            console.error('Status:', status);
            console.error('Response:', xhr.responseText);
            $('#chapterCommentsContainer').html('<p class="text-danger text-center">Không thể tải bình luận. Vui lòng thử lại sau.</p>');
        }
    });
}

// Function to submit a new comment
function submitComment(mangaId, chapterId, content) {
    // Make sure mangaId is a number
    if (typeof mangaId === 'string') {
        mangaId = parseInt(mangaId);
    }

    // Make sure chapterId is a number or null
    if (chapterId && typeof chapterId === 'string') {
        chapterId = parseInt(chapterId);
    }

    // Create the comment object with proper capitalization for .NET model binding
    const comment = {
        MangaId: mangaId,
        ChapterId: chapterId,
        Content: content
    };

    console.log('Submitting comment:', comment);

    // Prevent duplicate submissions
    const submitButton = chapterId ? '#submitChapterComment' : '#submitMangaComment';
    $(submitButton).prop('disabled', true);

    $.ajax({
        url: '/api/Comment',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(comment),
        success: function(response) {
            console.log('Comment submitted successfully:', response);

            // Clear the textarea
            $('#mangaCommentText, #chapterCommentText').val('');

            // Show success message
            showToast('Bình luận đã được gửi thành công', 'success');

            // Reload comments
            if (chapterId) {
                loadChapterComments(1);
            } else {
                loadMangaComments(1);
            }

            // Re-enable the submit button
            $(submitButton).prop('disabled', false);
        },
        error: function(xhr, status, error) {
            console.error('Error submitting comment:', error);
            console.error('Status:', status);
            console.error('Response:', xhr.responseText);

            let errorMessage = `Không thể gửi bình luận: ${xhr.status}`;
            showToast(errorMessage, 'error');

            // Re-enable the submit button
            $(submitButton).prop('disabled', false);
        }
    });
}

// Function to submit a reply to a comment
function submitReply(commentId, content) {
    // Create the reply object with proper capitalization for .NET model binding
    const reply = {
        Content: content
    };

    // Prevent duplicate submissions
    const submitButton = `.submit-reply-btn[data-comment-id="${commentId}"]`;
    $(submitButton).prop('disabled', true);

    $.ajax({
        url: `/api/Comment/${commentId}/reply`,
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(reply),
        success: function(response) {
            // Clear the textarea
            $(`#replyText-${commentId}`).val('');

            // Hide the reply form
            $(`#replyForm-${commentId}`).addClass('d-none');

            // Show success message
            showToast('Trả lời đã được gửi thành công', 'success');

            // Add the new reply to the DOM
            const replyHtml = renderReply(response);
            $(`#replies-${commentId}`).append(replyHtml);

            // Re-enable the submit button
            $(submitButton).prop('disabled', false);
        },
        error: function(xhr, status, error) {
            console.error('Error submitting reply:', error);
            console.error('Status:', status);
            console.error('Response:', xhr.responseText);

            showToast(`Không thể gửi trả lời: ${xhr.status}`, 'error');

            // Re-enable the submit button
            $(submitButton).prop('disabled', false);
        }
    });
}

// Function to submit a reaction (like/dislike)
function submitReaction(commentId, replyId, isLike) {
    // Create the reaction object with proper capitalization for .NET model binding
    const reaction = {
        CommentId: commentId || null,
        ReplyId: replyId || null,
        IsLike: isLike
    };

    console.log('Submitting reaction:', reaction);

    // Determine which button was clicked
    const selector = replyId ? `#reply-${replyId}` : `#comment-${commentId}`;
    const buttonType = isLike ? '.like-btn' : '.dislike-btn';
    const oppositeButtonType = isLike ? '.dislike-btn' : '.like-btn';
    const button = $(`${selector} ${buttonType}`);
    const oppositeButton = $(`${selector} ${oppositeButtonType}`);
    
    // Check if button is already active - if so, we're toggling it off
    const isActive = button.hasClass('active');
    
    // Update UI immediately for better user experience
    if (isActive) {
        // If already active, toggle it off
        button.removeClass('active');
        
        // Decrement count
        let countSpan = button.find('.like-count, .dislike-count');
        let currentCount = parseInt(countSpan.text()) || 0;
        if (currentCount > 0) {
            countSpan.text(currentCount - 1);
        }
    } else {
        // Toggle on this button, toggle off the opposite
        button.addClass('active');
        oppositeButton.removeClass('active');
        
        // Increment this count
        let countSpan = button.find('.like-count, .dislike-count');
        let currentCount = parseInt(countSpan.text()) || 0;
        countSpan.text(currentCount + 1);
        
        // If opposite button was active, decrement its count
        if (oppositeButton.hasClass('active')) {
            let oppositeCountSpan = oppositeButton.find('.like-count, .dislike-count');
            let oppositeCount = parseInt(oppositeCountSpan.text()) || 0;
            if (oppositeCount > 0) {
                oppositeCountSpan.text(oppositeCount - 1);
            }
        }
    }

    // Prevent duplicate submissions
    button.prop('disabled', true);

    $.ajax({
        url: '/api/Comment/reaction',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(reaction),
        success: function(response) {
            console.log('Reaction submitted successfully:', response);
            
            // Update the like/dislike counts from server to ensure accuracy
            if (commentId) {
                updateReactionCounts(commentId, null);
            } else if (replyId) {
                updateReactionCounts(null, replyId);
            }

            // Re-enable the button
            button.prop('disabled', false);
        },
        error: function(xhr, status, error) {
            console.error('Error submitting reaction:', error);
            console.error('Status:', status);
            console.error('Response:', xhr.responseText);

            let errorMessage = `Không thể gửi phản ứng: ${xhr.status}`;

            // Try to parse the error response for more details
            try {
                const response = JSON.parse(xhr.responseText);
                if (response.title) {
                    errorMessage += ` - ${response.title}`;
                }
                if (response.errors) {
                    const errorDetails = Object.entries(response.errors)
                        .map(([key, msgs]) => `${key}: ${msgs.join(', ')}`)
                        .join('; ');
                    console.error('Validation errors:', errorDetails);
                }
            } catch (e) {
                console.error('Could not parse error response:', e);
            }

            showToast(errorMessage, 'error');

            // Rollback UI changes since the server request failed
            if (isActive) {
                // Was toggling off, so re-add active class
                button.addClass('active');
            } else {
                // Was toggling on, so remove active class
                button.removeClass('active');
                // Restore opposite button if needed
                if (oppositeButton.hasClass('active')) {
                    oppositeButton.addClass('active');
                }
            }
            
            // Update counts from server to restore correct state
            if (commentId) {
                updateReactionCounts(commentId, null);
            } else if (replyId) {
                updateReactionCounts(null, replyId);
            }

            // Re-enable the button
            button.prop('disabled', false);
        }
    });
}

// Function to update reaction counts
function updateReactionCounts(commentId, replyId) {
    let url;
    let selector;

    if (commentId) {
        url = `/api/Comment/${commentId}/reactions`;
        selector = `#comment-${commentId}`;
    } else if (replyId) {
        url = `/api/Comment/reply/${replyId}/reactions`;
        selector = `#reply-${replyId}`;
    } else {
        return;
    }

    $.ajax({
        url: url,
        type: 'GET',
        success: function(response) {
            $(`${selector} .like-count`).text(response.likes);
            $(`${selector} .dislike-count`).text(response.dislikes);

            // Update active state of buttons
            if (response.userReaction) {
                if (response.userReaction.isLike) {
                    $(`${selector} .like-btn`).addClass('active');
                    $(`${selector} .dislike-btn`).removeClass('active');
                } else {
                    $(`${selector} .like-btn`).removeClass('active');
                    $(`${selector} .dislike-btn`).addClass('active');
                }
            } else {
                $(`${selector} .like-btn, ${selector} .dislike-btn`).removeClass('active');
            }
        },
        error: function(err) {
            console.error('Error updating reaction counts:', err);
        }
    });
}

// Function to render a comment
function renderComment(comment, showChapter = false) {
    console.log('Rendering comment:', comment);

    // Check if this is a reference object
    if (comment.$ref) {
        console.warn('Received a reference object instead of a comment:', comment);
        return ''; // Skip rendering reference objects
    }

    // Handle missing properties with defaults
    const createdAt = comment.createdAt ? new Date(comment.createdAt).toLocaleString('vi-VN') : 'Unknown date';

    // Handle missing reactions
    let likesCount = 0;
    let dislikesCount = 0;

    if (comment.reactions && Array.isArray(comment.reactions)) {
        likesCount = comment.reactions.filter(r => r.isLike).length;
        dislikesCount = comment.reactions.filter(r => !r.isLike).length;
    }

    // Handle missing user or reference-based user
    let username = 'Unknown user';
    let avatarUrl = '/images/default-avatar.png';

    // Check if user is a reference object
    if (comment.user && comment.user.$ref) {
        console.warn('User is a reference object, trying to resolve manually');
        // Try to find the user object in the comment itself
        if (comment.userId) {
            // Try to find a user with this ID in the comment
            const userProps = Object.entries(comment).find(([_, value]) =>
                value && typeof value === 'object' && value.userId === comment.userId && value.username);

            if (userProps && userProps[1]) {
                username = userProps[1].username || 'Unknown user';
                avatarUrl = userProps[1].avatarUrl || '/images/default-avatar.png';
                console.log('Manually resolved user:', username);
            } else {
                // Try to fetch user information from the server
                console.log('Fetching user information for userId:', comment.userId);

                // Make a synchronous request to get user info
                $.ajax({
                    url: `/api/User/${comment.userId}`,
                    type: 'GET',
                    async: false,
                    success: function(user) {
                        if (user && user.username) {
                            username = user.username;
                            avatarUrl = user.avatarUrl || '/images/default-avatar.png';
                            console.log('Fetched user information:', username);
                        }
                    },
                    error: function(_, __, error) {
                        console.error('Error fetching user information:', error);
                    }
                });
            }
        }
    } else if (comment.user) {
        // Normal user object
        username = comment.user.username || 'Unknown user';
        avatarUrl = comment.user.avatarUrl || '/images/default-avatar.png';
    } else if (comment.userId) {
        // No user object, but we have userId
        console.log('No user object, but have userId:', comment.userId);

        // Make a synchronous request to get user info
        $.ajax({
            url: `/api/User/${comment.userId}`,
            type: 'GET',
            async: false,
            success: function(user) {
                if (user && user.username) {
                    username = user.username;
                    avatarUrl = user.avatarUrl || '/images/default-avatar.png';
                    console.log('Fetched user information:', username);
                }
            },
            error: function(_, __, error) {
                console.error('Error fetching user information:', error);
            }
        });
    }

    // Handle missing chapter
    let chapterInfo = '';
    if (showChapter && comment.chapter) {
        const chapterNumber = comment.chapter.chapterNumber || 'Unknown';
        chapterInfo = `
            <div class="comment-chapter">
                <a href="/chapter/read/${comment.chapterId}" class="badge bg-light text-dark text-decoration-none">
                    <i class="fas fa-book-open me-1"></i> Chapter ${chapterNumber}
                </a>
            </div>
        `;
    }

    // Handle missing replies
    let repliesHtml = '';
    if (comment.replies && Array.isArray(comment.replies) && comment.replies.length > 0) {
        repliesHtml = '<div class="comment-replies mt-3">';
        comment.replies.forEach(reply => {
            repliesHtml += renderReply(reply);
        });
        repliesHtml += '</div>';
    }

    // Handle reply form
    const replyForm = typeof isAuthenticated !== 'undefined' && isAuthenticated ? `
        <div id="replyForm-${comment.commentId}" class="reply-form mt-2 d-none">
            <div class="input-group">
                <textarea id="replyText-${comment.commentId}" class="form-control" rows="2" placeholder="Viết trả lời của bạn..."></textarea>
                <button class="btn btn-primary submit-reply-btn" data-comment-id="${comment.commentId}">Gửi</button>
            </div>
        </div>
    ` : '';

    // Use the username and avatarUrl variables we defined earlier
    return `
        <div class="comment-item mb-3" id="comment-${comment.commentId}">
            <div class="d-flex">
                <img src="${avatarUrl}" alt="${username}" class="avatar-sm rounded-circle me-2">
                <div class="flex-grow-1">
                    <div class="comment-header">
                        <span class="comment-author">${username}</span>
                        <span class="comment-meta text-muted ms-2">${createdAt}</span>
                        ${chapterInfo}
                    </div>
                    <div class="comment-content">
                        ${comment.content || ''}
                    </div>
                    <div class="comment-footer d-flex align-items-center">
                        <button class="btn btn-sm btn-link text-decoration-none reaction-btn like-btn ${comment.userReaction && comment.userReaction.isLike ? 'active' : ''}" data-comment-id="${comment.commentId}">
                            <i class="fas fa-thumbs-up"></i> <span class="like-count">${likesCount}</span>
                        </button>
                        <button class="btn btn-sm btn-link text-decoration-none reaction-btn dislike-btn ${comment.userReaction && !comment.userReaction.isLike ? 'active' : ''}" data-comment-id="${comment.commentId}">
                            <i class="fas fa-thumbs-down"></i> <span class="dislike-count">${dislikesCount}</span>
                        </button>
                        ${typeof isAuthenticated !== 'undefined' && isAuthenticated ? `
                            <button class="btn btn-sm btn-link text-decoration-none reply-btn" data-comment-id="${comment.commentId}">
                                <i class="fas fa-reply"></i> Trả lời
                            </button>
                        ` : ''}
                    </div>
                    ${replyForm}
                    <div id="replies-${comment.commentId}">
                        ${repliesHtml}
                    </div>
                </div>
            </div>
        </div>
    `;
}

// Function to render a reply
function renderReply(reply) {
    console.log('Rendering reply:', reply);

    // Check if this is a reference object
    if (reply.$ref) {
        console.warn('Received a reference object instead of a reply:', reply);
        return ''; // Skip rendering reference objects
    }

    // Handle missing properties with defaults
    const createdAt = reply.createdAt ? new Date(reply.createdAt).toLocaleString('vi-VN') : 'Unknown date';

    // Handle missing reactions
    let likesCount = 0;
    let dislikesCount = 0;

    if (reply.reactions && Array.isArray(reply.reactions)) {
        likesCount = reply.reactions.filter(r => r.isLike).length;
        dislikesCount = reply.reactions.filter(r => !r.isLike).length;
    }

    // Handle missing user or reference-based user
    let username = 'Unknown user';
    let avatarUrl = '/images/default-avatar.png';

    // Check if user is a reference object
    if (reply.user && reply.user.$ref) {
        console.warn('Reply user is a reference object, trying to resolve manually');
        // Try to find the user object in the reply itself
        if (reply.userId) {
            // Try to find a user with this ID in the reply
            const userProps = Object.entries(reply).find(([_, value]) =>
                value && typeof value === 'object' && value.userId === reply.userId && value.username);

            if (userProps && userProps[1]) {
                username = userProps[1].username || 'Unknown user';
                avatarUrl = userProps[1].avatarUrl || '/images/default-avatar.png';
                console.log('Manually resolved reply user:', username);
            } else {
                // Try to fetch user information from the server
                console.log('Fetching user information for reply userId:', reply.userId);

                // Make a synchronous request to get user info
                $.ajax({
                    url: `/api/User/${reply.userId}`,
                    type: 'GET',
                    async: false,
                    success: function(user) {
                        if (user && user.username) {
                            username = user.username;
                            avatarUrl = user.avatarUrl || '/images/default-avatar.png';
                            console.log('Fetched reply user information:', username);
                        }
                    },
                    error: function(_, __, error) {
                        console.error('Error fetching reply user information:', error);
                    }
                });
            }
        }
    } else if (reply.user) {
        // Normal user object
        username = reply.user.username || 'Unknown user';
        avatarUrl = reply.user.avatarUrl || '/images/default-avatar.png';
    } else if (reply.userId) {
        // No user object in reply, but we have userId
        console.log('No user object in reply, but have userId:', reply.userId);

        // Make a synchronous request to get user info
        $.ajax({
            url: `/api/User/${reply.userId}`,
            type: 'GET',
            async: false,
            success: function(user) {
                if (user && user.username) {
                    username = user.username;
                    avatarUrl = user.avatarUrl || '/images/default-avatar.png';
                    console.log('Fetched reply user information:', username);
                }
            },
            error: function(_, __, error) {
                console.error('Error fetching reply user information:', error);
            }
        });
    }

    // Ensure we have a valid replyId for the buttons
    const replyId = reply.replyId || 0;
    
    // Check if we have userReaction data
    const hasUserLike = reply.userReaction && reply.userReaction.isLike;
    const hasUserDislike = reply.userReaction && !reply.userReaction.isLike;

    return `
        <div class="reply-item mt-2" id="reply-${replyId}">
            <div class="d-flex">
                <img src="${avatarUrl}" alt="${username}" class="avatar-xs rounded-circle me-2">
                <div class="flex-grow-1">
                    <div class="reply-header">
                        <span class="reply-author">${username}</span>
                        <span class="reply-meta text-muted ms-2">${createdAt}</span>
                    </div>
                    <div class="reply-content">
                        ${reply.content || ''}
                    </div>
                    <div class="reply-footer d-flex align-items-center">
                        <button class="btn btn-sm btn-link text-decoration-none reaction-btn like-btn ${hasUserLike ? 'active' : ''}" data-reply-id="${replyId}">
                            <i class="fas fa-thumbs-up"></i> <span class="like-count">${likesCount}</span>
                        </button>
                        <button class="btn btn-sm btn-link text-decoration-none reaction-btn dislike-btn ${hasUserDislike ? 'active' : ''}" data-reply-id="${replyId}">
                            <i class="fas fa-thumbs-down"></i> <span class="dislike-count">${dislikesCount}</span>
                        </button>
                    </div>
                </div>
            </div>
        </div>
    `;
}

// Function to add new replies to the comment
function addReplyToCommentDisplay(commentId, reply) {
    // Create HTML for the reply
    const replyHtml = renderReply(reply);
    
    // Get the replies container
    const repliesContainer = $(`#replies-${commentId}`);
    
    // If the container is empty, create a comment-replies div
    if (repliesContainer.children().length === 0) {
        repliesContainer.html('<div class="comment-replies mt-3"></div>');
    }
    
    // Append the reply to the container
    repliesContainer.children('.comment-replies').append(replyHtml);
    
    // Update reaction count for the new reply
    if (reply.replyId) {
        // Make sure we update the reaction counts for the reply
        updateReactionCounts(null, reply.replyId);
    }
}

// Function to show toast notifications
function showToast(message, type = 'info') {
    const toast = `
        <div class="toast align-items-center text-white bg-${type === 'success' ? 'success' : type === 'error' ? 'danger' : type === 'warning' ? 'warning' : 'info'}" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body">
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        </div>
    `;

    const toastContainer = document.getElementById('toastContainer');
    if (!toastContainer) {
        const container = document.createElement('div');
        container.id = 'toastContainer';
        container.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        document.body.appendChild(container);
    }

    $('#toastContainer').append(toast);
    const toastElement = new bootstrap.Toast($('.toast').last(), { delay: 3000 });
    toastElement.show();
}
