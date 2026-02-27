using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mangareading.Models
{
    [Table("Comments")]
    public class Comment
    {
        public Comment()
        {
            // Initialize collections to avoid null reference exceptions
            Replies = new List<CommentReply>();
            Reactions = new List<CommentReaction>();
        }

        [Key]
        public int CommentId { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Foreign keys
        public int UserId { get; set; }

        public int MangaId { get; set; }

        public int? ChapterId { get; set; } // Nullable for manga-level comments

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("MangaId")]
        public virtual Manga Manga { get; set; }

        [ForeignKey("ChapterId")]
        public virtual Chapter Chapter { get; set; }

        // Related collections
        public virtual ICollection<CommentReply> Replies { get; set; }
        public virtual ICollection<CommentReaction> Reactions { get; set; }
    }
}
