using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Mangareading.Models;
using Mangareading.Repositories;

namespace Mangareading.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly ICommentRepository _commentRepository;
        private readonly YourDbContext _context;
        private readonly ILogger<CommentController> _logger;

        public CommentController(
            ICommentRepository commentRepository,
            YourDbContext context,
            ILogger<CommentController> logger)
        {
            _commentRepository = commentRepository;
            _context = context;
            _logger = logger;
        }

        // GET: api/Comment/latest
        [HttpGet("latest")]
        public async Task<ActionResult<List<Comment>>> GetLatestComments([FromQuery] int count = 5)
        {
            try
            {
                var latestComments = await _context.Comments
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(count)
                    .Include(c => c.User)
                    .Include(c => c.Manga)
                    .Include(c => c.Chapter)
                    .ToListAsync();

                return Ok(latestComments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest comments");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/Comment/manga/{mangaId}
        [HttpGet("manga/{mangaId}")]
        public async Task<ActionResult<List<Comment>>> GetMangaComments(int mangaId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var comments = await _commentRepository.GetMangaCommentsAsync(mangaId, page, pageSize);
                return Ok(comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting comments for manga {mangaId}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/Comment/chapter/{chapterId}
        [HttpGet("chapter/{chapterId}")]
        public async Task<ActionResult<List<Comment>>> GetChapterComments(int chapterId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                _logger.LogInformation($"Getting comments for chapter {chapterId}, page {page}, pageSize {pageSize}");
                var comments = await _commentRepository.GetChapterCommentsAsync(chapterId, page, pageSize);
                return Ok(comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting comments for chapter {chapterId}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/Comment/manga/{mangaId}/chapters
        [HttpGet("manga/{mangaId}/chapters")]
        public async Task<ActionResult<List<Comment>>> GetMangaChapterComments(int mangaId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                _logger.LogInformation($"Getting chapter comments for manga {mangaId}, page {page}, pageSize {pageSize}");
                // Get all comments for this manga that have a chapter ID
                var comments = await _context.Comments
                    .Where(c => c.MangaId == mangaId && c.ChapterId != null)
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Include(c => c.User)
                    .Include(c => c.Chapter)
                    .Include(c => c.Replies)
                        .ThenInclude(r => r.User)
                    .Include(c => c.Reactions)
                    .ToListAsync();

                return Ok(comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting chapter comments for manga {mangaId}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/Comment/{commentId}
        [HttpGet("{commentId}")]
        public async Task<ActionResult<Comment>> GetComment(int commentId)
        {
            try
            {
                var comment = await _commentRepository.GetCommentByIdAsync(commentId);
                if (comment == null)
                    return NotFound();

                return Ok(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting comment {commentId}");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/Comment
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Comment>> AddComment([FromBody] CommentDTO commentDto)
        {
            try
            {
                // Basic validation

                // Check ModelState
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    _logger.LogWarning($"ModelState is invalid: {errors}");
                    return BadRequest(new { title = "Validation Failed", errors = ModelState });
                }

                // Check if comment DTO is null
                if (commentDto == null)
                {
                    _logger.LogWarning("Comment DTO is null");
                    return BadRequest(new { title = "Invalid Request", detail = "Comment cannot be null" });
                }

                // Check MangaId
                if (commentDto.MangaId <= 0)
                {
                    _logger.LogWarning($"Invalid MangaId: {commentDto.MangaId}");
                    return BadRequest(new { title = "Invalid Request", detail = $"MangaId must be greater than 0, received: {commentDto.MangaId}" });
                }

                // Check Content
                if (string.IsNullOrWhiteSpace(commentDto.Content))
                {
                    _logger.LogWarning("Comment content is empty");
                    return BadRequest(new { title = "Invalid Request", detail = "Comment content cannot be empty" });
                }

                // Check if manga exists
                var manga = await _context.Mangas.FindAsync(commentDto.MangaId);
                if (manga == null)
                {
                    _logger.LogWarning($"Manga with ID {commentDto.MangaId} not found");
                    return BadRequest(new { title = "Invalid Request", detail = $"Manga with ID {commentDto.MangaId} not found" });
                }

                // Check if chapter exists (if provided)
                if (commentDto.ChapterId.HasValue)
                {
                    var chapter = await _context.Chapters.FindAsync(commentDto.ChapterId.Value);
                    if (chapter == null)
                    {
                        _logger.LogWarning($"Chapter with ID {commentDto.ChapterId} not found");
                        return BadRequest(new { title = "Invalid Request", detail = $"Chapter with ID {commentDto.ChapterId} not found" });
                    }
                }

                // Create a new Comment entity from the DTO
                var comment = new Comment
                {
                    MangaId = commentDto.MangaId,
                    ChapterId = commentDto.ChapterId,
                    Content = commentDto.Content,
                    CreatedAt = DateTime.UtcNow
                };

                // Set user ID from authenticated user
                comment.UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var newComment = await _commentRepository.AddCommentAsync(comment);
                return CreatedAtAction(nameof(GetComment), new { commentId = newComment.CommentId }, newComment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding comment: {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT: api/Comment/{commentId}
        [HttpPut("{commentId}")]
        [Authorize]
        public async Task<IActionResult> UpdateComment(int commentId, [FromBody] Comment comment)
        {
            try
            {
                if (commentId != comment.CommentId)
                    return BadRequest("Comment ID mismatch");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Verify user owns the comment
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var existingComment = await _commentRepository.GetCommentByIdAsync(commentId);

                if (existingComment == null)
                    return NotFound();

                if (existingComment.UserId != userId && !User.IsInRole("Admin"))
                    return Forbid();

                var result = await _commentRepository.UpdateCommentAsync(comment);
                if (!result)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating comment {commentId}");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/Comment/{commentId}
        [HttpDelete("{commentId}")]
        [Authorize]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            try
            {
                // Verify user owns the comment
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var existingComment = await _commentRepository.GetCommentByIdAsync(commentId);

                if (existingComment == null)
                    return NotFound();

                if (existingComment.UserId != userId && !User.IsInRole("Admin"))
                    return Forbid();

                var result = await _commentRepository.DeleteCommentAsync(commentId);
                if (!result)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting comment {commentId}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/Comment/{commentId}/replies
        [HttpGet("{commentId}/replies")]
        public async Task<ActionResult<List<CommentReply>>> GetCommentReplies(int commentId)
        {
            try
            {
                var replies = await _commentRepository.GetCommentRepliesAsync(commentId);
                return Ok(replies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting replies for comment {commentId}");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/Comment/{commentId}/reply
        [HttpPost("{commentId}/reply")]
        [Authorize]
        public async Task<ActionResult<CommentReply>> AddReply(int commentId, [FromBody] ReplyDTO replyDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Check if the comment exists
                var comment = await _commentRepository.GetCommentByIdAsync(commentId);
                if (comment == null)
                    return NotFound($"Comment with ID {commentId} not found");

                // Create a new CommentReply entity from the DTO
                var reply = new CommentReply
                {
                    Content = replyDto.Content,
                    CommentId = commentId,
                    CreatedAt = DateTime.UtcNow
                };

                // Set user ID from authenticated user
                reply.UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var newReply = await _commentRepository.AddReplyAsync(reply);

                // Load the user for the response
                await _context.Entry(newReply).Reference(r => r.User).LoadAsync();

                return CreatedAtAction(nameof(GetCommentReplies), new { commentId }, newReply);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding reply to comment {commentId}");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/Comment/reply/{replyId}
        [HttpPut("reply/{replyId}")]
        [Authorize]
        public async Task<IActionResult> UpdateReply(int replyId, [FromBody] CommentReply reply)
        {
            try
            {
                if (replyId != reply.ReplyId)
                    return BadRequest("Reply ID mismatch");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Verify user owns the reply
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var existingReply = await _context.CommentReplies.FindAsync(replyId);

                if (existingReply == null)
                    return NotFound();

                if (existingReply.UserId != userId && !User.IsInRole("Admin"))
                    return Forbid();

                var result = await _commentRepository.UpdateReplyAsync(reply);
                if (!result)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating reply {replyId}");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/Comment/reply/{replyId}
        [HttpDelete("reply/{replyId}")]
        [Authorize]
        public async Task<IActionResult> DeleteReply(int replyId)
        {
            try
            {
                // Verify user owns the reply
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var existingReply = await _context.CommentReplies.FindAsync(replyId);

                if (existingReply == null)
                    return NotFound();

                if (existingReply.UserId != userId && !User.IsInRole("Admin"))
                    return Forbid();

                var result = await _commentRepository.DeleteReplyAsync(replyId);
                if (!result)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting reply {replyId}");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/Comment/reaction
        [HttpPost("reaction")]
        [Authorize]
        public async Task<ActionResult<CommentReaction>> AddReaction([FromBody] ReactionDTO reactionDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Validate that either CommentId or ReplyId is provided, but not both
                if ((reactionDto.CommentId == null && reactionDto.ReplyId == null) ||
                    (reactionDto.CommentId != null && reactionDto.ReplyId != null))
                {
                    return BadRequest("Either CommentId or ReplyId must be provided, but not both.");
                }

                // Check if the comment or reply exists
                if (reactionDto.CommentId != null)
                {
                    var comment = await _commentRepository.GetCommentByIdAsync(reactionDto.CommentId.Value);
                    if (comment == null)
                        return NotFound($"Comment with ID {reactionDto.CommentId} not found");
                }
                else if (reactionDto.ReplyId != null)
                {
                    var reply = await _context.CommentReplies.FindAsync(reactionDto.ReplyId.Value);
                    if (reply == null)
                        return NotFound($"Reply with ID {reactionDto.ReplyId} not found");
                }

                // Create a new CommentReaction entity from the DTO
                var reaction = new CommentReaction
                {
                    CommentId = reactionDto.CommentId,
                    ReplyId = reactionDto.ReplyId,
                    IsLike = reactionDto.IsLike,
                    CreatedAt = DateTime.UtcNow
                };

                // Set user ID from authenticated user
                reaction.UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var newReaction = await _commentRepository.AddReactionAsync(reaction);
                return Ok(newReaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding reaction");
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/Comment/reaction/{reactionId}
        [HttpDelete("reaction/{reactionId}")]
        [Authorize]
        public async Task<IActionResult> DeleteReaction(int reactionId)
        {
            try
            {
                // Verify user owns the reaction
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var existingReaction = await _context.CommentReactions.FindAsync(reactionId);

                if (existingReaction == null)
                    return NotFound();

                if (existingReaction.UserId != userId && !User.IsInRole("Admin"))
                    return Forbid();

                var result = await _commentRepository.DeleteReactionAsync(reactionId);
                if (!result)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting reaction {reactionId}");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/Comment/{commentId}/reactions
        [HttpGet("{commentId}/reactions")]
        public async Task<ActionResult> GetCommentReactions(int commentId)
        {
            try
            {
                var likes = await _commentRepository.GetCommentLikesCountAsync(commentId);
                var dislikes = await _commentRepository.GetCommentDislikesCountAsync(commentId);

                // Get user's reaction if authenticated
                CommentReaction userReaction = null;
                if (User.Identity.IsAuthenticated)
                {
                    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                    userReaction = await _commentRepository.GetUserReactionAsync(userId, commentId, null);
                }

                return Ok(new { likes, dislikes, userReaction });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting reactions for comment {commentId}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/Comment/reply/{replyId}/reactions
        [HttpGet("reply/{replyId}/reactions")]
        public async Task<ActionResult> GetReplyReactions(int replyId)
        {
            try
            {
                var likes = await _commentRepository.GetReplyLikesCountAsync(replyId);
                var dislikes = await _commentRepository.GetReplyDislikesCountAsync(replyId);

                // Get user's reaction if authenticated
                CommentReaction userReaction = null;
                if (User.Identity.IsAuthenticated)
                {
                    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                    userReaction = await _commentRepository.GetUserReactionAsync(userId, null, replyId);
                }

                return Ok(new { likes, dislikes, userReaction });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting reactions for reply {replyId}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
