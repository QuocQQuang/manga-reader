using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mangareading.Models
{
    [Table("Chapters")]
    public class Chapter
    {
        public Chapter()
        {
            // Khởi tạo các collection để tránh null reference exception
            Pages = new List<Page>();
            Comments = new List<Comment>();
        }

        [Key]
        public int ChapterId { get; set; }

        public int? MangaId { get; set; }

        [StringLength(255)]
        public string? Title { get; set; }

        public decimal ChapterNumber { get; set; }

        [StringLength(10)]
        public string? LanguageCode { get; set; }

        public int? SourceId { get; set; }

        [StringLength(100)]
        public string? ExternalId { get; set; }

        public DateTime UploadDate { get; set; }

        [ForeignKey("MangaId")]
        public virtual Manga? Manga { get; set; }

        [ForeignKey("SourceId")]
        public virtual Source? Source { get; set; }

        public virtual ICollection<Page> Pages { get; set; }

        public virtual ICollection<Comment> Comments { get; set; }
    }
}