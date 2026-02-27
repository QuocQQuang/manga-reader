using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mangareading.Models
{
    [Table("CommentReactions")]
    public class CommentReaction
    {
        [Key]
        public int ReactionId { get; set; }

        public bool IsLike { get; set; } // true for like, false for dislike

        public DateTime CreatedAt { get; set; }

        // Foreign keys
        public int UserId { get; set; }

        public int? CommentId { get; set; } // Nullable because it can be either a comment or reply reaction

        public int? ReplyId { get; set; } // Nullable because it can be either a comment or reply reaction

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("CommentId")]
        public virtual Comment Comment { get; set; }

        [ForeignKey("ReplyId")]
        public virtual CommentReply Reply { get; set; }
    }
}
