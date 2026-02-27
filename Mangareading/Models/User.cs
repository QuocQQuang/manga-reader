using System;
using System.Collections.Generic;

namespace Mangareading.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string? AvatarUrl { get; set; } // Explicitly marking as nullable
        public bool IsAdmin { get; set; }
        public bool IsActive { get; set; } = true; // By default, user accounts are active
        public DateTime CreatedAt { get; set; }
        public string ThemePreference { get; set; } = "light"; // Default to light theme

        public ICollection<ReadingProgress> ReadingProgresses { get; set; }
        public ICollection<Favorite> Favorites { get; set; }
        public ICollection<ReadingHistory> ReadingHistories { get; set; }

        // Comment-related navigation properties
        public ICollection<Comment> Comments { get; set; }
        public ICollection<CommentReply> CommentReplies { get; set; }
        public ICollection<CommentReaction> CommentReactions { get; set; }
    }
}