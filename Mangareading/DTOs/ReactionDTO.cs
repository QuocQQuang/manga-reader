using System.ComponentModel.DataAnnotations;

namespace Mangareading.Models
{
    public class ReactionDTO
    {
        public int? CommentId { get; set; }
        
        public int? ReplyId { get; set; }
        
        [Required]
        public bool IsLike { get; set; }
    }
}
