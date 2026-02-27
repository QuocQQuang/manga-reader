using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Mangareading.Models
{
    public class Author
    {
        [Key]
        public int Id { get; set; }
        
        [Required, MaxLength(36)]
        public string MangadexId { get; set; }
        
        [Required, MaxLength(255)]
        public string Name { get; set; }
        
        [MaxLength(50)]
        public string Role { get; set; } // "author" or "artist"
        
        // Navigation property
        public virtual ICollection<Manga> Mangas { get; set; }
    }
}