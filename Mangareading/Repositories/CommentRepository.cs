using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mangareading.Models;

namespace Mangareading.Repositories
{
    public interface ICommentRepository
    {
        // Comment methods
        Task<List<Comment>> GetMangaCommentsAsync(int mangaId, int page = 1, int pageSize = 10);
        Task<List<Comment>> GetChapterCommentsAsync(int chapterId, int page = 1, int pageSize = 10);
        Task<Comment> GetCommentByIdAsync(int commentId);
        Task<Comment> AddCommentAsync(Comment comment);
        Task<bool> UpdateCommentAsync(Comment comment);
        Task<bool> DeleteCommentAsync(int commentId);

        // Reply methods
        Task<List<CommentReply>> GetCommentRepliesAsync(int commentId);
        Task<CommentReply> AddReplyAsync(CommentReply reply);
        Task<bool> UpdateReplyAsync(CommentReply reply);
        Task<bool> DeleteReplyAsync(int replyId);

        // Reaction methods
        Task<int> GetCommentLikesCountAsync(int commentId);
        Task<int> GetCommentDislikesCountAsync(int commentId);
        Task<int> GetReplyLikesCountAsync(int replyId);
        Task<int> GetReplyDislikesCountAsync(int replyId);
        Task<CommentReaction> AddReactionAsync(CommentReaction reaction);
        Task<bool> UpdateReactionAsync(CommentReaction reaction);
        Task<bool> DeleteReactionAsync(int reactionId);
        Task<CommentReaction> GetUserReactionAsync(int userId, int? commentId, int? replyId);

        // Method to get latest comments
        Task<List<Comment>> GetLatestCommentsAsync(int count);
    }

    public class CommentRepository : ICommentRepository
    {
        private readonly YourDbContext _context;

        public CommentRepository(YourDbContext context)
        {
            _context = context;
        }

        // Comment methods
        public async Task<List<Comment>> GetMangaCommentsAsync(int mangaId, int page = 1, int pageSize = 10)
        {
            return await _context.Comments
                .Where(c => c.MangaId == mangaId)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(c => c.User)
                .Include(c => c.Chapter)
                .Include(c => c.Replies)
                    .ThenInclude(r => r.User)
                .Include(c => c.Reactions)
                .ToListAsync();
        }

        public async Task<List<Comment>> GetChapterCommentsAsync(int chapterId, int page = 1, int pageSize = 10)
        {
            return await _context.Comments
                .Where(c => c.ChapterId == chapterId)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(c => c.User)
                .Include(c => c.Replies)
                    .ThenInclude(r => r.User)
                .Include(c => c.Reactions)
                .ToListAsync();
        }

        public async Task<Comment> GetCommentByIdAsync(int commentId)
        {
            return await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Chapter)
                .Include(c => c.Replies)
                    .ThenInclude(r => r.User)
                .Include(c => c.Reactions)
                .FirstOrDefaultAsync(c => c.CommentId == commentId);
        }

        public async Task<Comment> AddCommentAsync(Comment comment)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrEmpty(comment.Content))
                    throw new ArgumentException("Comment content cannot be empty");

                if (comment.MangaId <= 0)
                    throw new ArgumentException("Invalid MangaId");

                if (comment.UserId <= 0)
                    throw new ArgumentException("Invalid UserId");

                // Set creation time
                comment.CreatedAt = DateTime.UtcNow;

                // Add to database
                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                return comment;
            }
            catch (Exception ex)
            {
                // Log the exception details
                Console.WriteLine($"Error in AddCommentAsync: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");

                // Rethrow to be handled by the controller
                throw;
            }
        }

        public async Task<bool> UpdateCommentAsync(Comment comment)
        {
            var existingComment = await _context.Comments.FindAsync(comment.CommentId);
            if (existingComment == null)
                return false;

            existingComment.Content = comment.Content;
            existingComment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCommentAsync(int commentId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null)
                return false;

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return true;
        }

        // Reply methods
        public async Task<List<CommentReply>> GetCommentRepliesAsync(int commentId)
        {
            return await _context.CommentReplies
                .Where(r => r.CommentId == commentId)
                .OrderBy(r => r.CreatedAt)
                .Include(r => r.User)
                .Include(r => r.Reactions)
                .ToListAsync();
        }

        public async Task<CommentReply> AddReplyAsync(CommentReply reply)
        {
            reply.CreatedAt = DateTime.UtcNow;
            _context.CommentReplies.Add(reply);
            await _context.SaveChangesAsync();
            return reply;
        }

        public async Task<bool> UpdateReplyAsync(CommentReply reply)
        {
            var existingReply = await _context.CommentReplies.FindAsync(reply.ReplyId);
            if (existingReply == null)
                return false;

            existingReply.Content = reply.Content;
            existingReply.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteReplyAsync(int replyId)
        {
            var reply = await _context.CommentReplies.FindAsync(replyId);
            if (reply == null)
                return false;

            _context.CommentReplies.Remove(reply);
            await _context.SaveChangesAsync();
            return true;
        }

        // Reaction methods
        public async Task<int> GetCommentLikesCountAsync(int commentId)
        {
            return await _context.CommentReactions
                .CountAsync(r => r.CommentId == commentId && r.IsLike);
        }

        public async Task<int> GetCommentDislikesCountAsync(int commentId)
        {
            return await _context.CommentReactions
                .CountAsync(r => r.CommentId == commentId && !r.IsLike);
        }

        public async Task<int> GetReplyLikesCountAsync(int replyId)
        {
            return await _context.CommentReactions
                .CountAsync(r => r.ReplyId == replyId && r.IsLike);
        }

        public async Task<int> GetReplyDislikesCountAsync(int replyId)
        {
            return await _context.CommentReactions
                .CountAsync(r => r.ReplyId == replyId && !r.IsLike);
        }

        public async Task<CommentReaction> AddReactionAsync(CommentReaction reaction)
        {
            // Check if user already reacted to this comment/reply
            var existingReaction = await GetUserReactionAsync(
                reaction.UserId,
                reaction.CommentId,
                reaction.ReplyId);

            if (existingReaction != null)
            {
                // Update existing reaction
                existingReaction.IsLike = reaction.IsLike;
                await _context.SaveChangesAsync();
                return existingReaction;
            }

            // Add new reaction
            reaction.CreatedAt = DateTime.UtcNow;
            _context.CommentReactions.Add(reaction);
            await _context.SaveChangesAsync();
            return reaction;
        }

        public async Task<bool> UpdateReactionAsync(CommentReaction reaction)
        {
            var existingReaction = await _context.CommentReactions.FindAsync(reaction.ReactionId);
            if (existingReaction == null)
                return false;

            existingReaction.IsLike = reaction.IsLike;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteReactionAsync(int reactionId)
        {
            var reaction = await _context.CommentReactions.FindAsync(reactionId);
            if (reaction == null)
                return false;

            _context.CommentReactions.Remove(reaction);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<CommentReaction> GetUserReactionAsync(int userId, int? commentId, int? replyId)
        {
            if (commentId.HasValue)
            {
                return await _context.CommentReactions
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.CommentId == commentId);
            }
            else if (replyId.HasValue)
            {
                return await _context.CommentReactions
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.ReplyId == replyId);
            }

            return null;
        }

        // Method to get latest comments implementation
        public async Task<List<Comment>> GetLatestCommentsAsync(int count)
        {
            return await _context.Comments
                .OrderByDescending(c => c.CreatedAt)
                .Take(count)
                .Include(c => c.User) // Include User information
                .Include(c => c.Manga) // Include Manga information
                // .Include(c => c.Chapter) // Optionally include Chapter if needed
                .ToListAsync();
        }
    }
}
