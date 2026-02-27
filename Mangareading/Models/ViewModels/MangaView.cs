using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mangareading.Models
{
    [Table("MangaViews")]
    public class MangaView
    {
        [Key]
        public int ViewId { get; set; }

        public int MangaId { get; set; }
        
        public int ChapterId { get; set; }
        
        public int? UserId { get; set; }  // Nullable for anonymous views
        
        public string? IpAddress { get; set; }  // For tracking anonymous views
        
        [Required]
        public DateTime ViewedAt { get; set; }

        // Navigation properties
        [ForeignKey("MangaId")]
        public virtual Manga Manga { get; set; } = null!;
        
        [ForeignKey("ChapterId")]
        public virtual Chapter Chapter { get; set; } = null!;
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}