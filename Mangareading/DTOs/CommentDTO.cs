using System.ComponentModel.DataAnnotations;

namespace Mangareading.Models
{
    public class CommentDTO
    {
        [Required]
        public int MangaId { get; set; }

        public int? ChapterId { get; set; }

        [Required]
        public string Content { get; set; }
    }
}
