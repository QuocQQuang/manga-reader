using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mangareading.Models
{
    [Table("CommentReplies")]
    public class CommentReply
    {
        public CommentReply()
        {
            // Initialize collections to avoid null reference exceptions
            Reactions = new List<CommentReaction>();
        }

        [Key]
        public int ReplyId { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Foreign keys
        public int CommentId { get; set; }

        public int UserId { get; set; }

        // Navigation properties
        [ForeignKey("CommentId")]
        public virtual Comment Comment { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        // Related collections
        public virtual ICollection<CommentReaction> Reactions { get; set; }
    }
}
