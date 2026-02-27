using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace Mangareading.Models
{
    [Table("Pages")]
    public class Page
    {
        [Key]
        public int PageId { get; set; }

        [Required]
        public int ChapterId { get; set; }

        [Required]
        public int PageNumber { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [StringLength(100)]
        public string? ImageHash { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        public int? FileSize { get; set; }
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ChapterId")]
        public virtual Chapter? Chapter { get; set; }
    }
}