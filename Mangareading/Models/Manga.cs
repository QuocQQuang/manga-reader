using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mangareading.Models
{
    public class Manga
    {
        [Key]
        public int MangaId { get; set; }

        [Required(ErrorMessage = "Tên truyện là bắt buộc.")]
        [Display(Name = "Tên truyện")]
        public string Title { get; set; }

        [Display(Name = "Tên khác")]
        public string? AlternativeTitle { get; set; }

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Ảnh bìa")]
        public string CoverUrl { get; set; }

        [Display(Name = "Tác giả")]
        public string Author { get; set; }

        [Display(Name = "Họa sĩ")]
        public string Artist { get; set; }

        [Display(Name = "Trạng thái")]
        public string Status { get; set; }

        [Display(Name = "Năm phát hành")]
        public int? PublicationYear { get; set; }

        [Display(Name = "Ngôn ngữ gốc")]
        public string? OriginalLanguage { get; set; }

        [Required(ErrorMessage = "Nguồn là bắt buộc.")]
        [Display(Name = "Nguồn")]
        public int SourceId { get; set; } // Foreign key to Sources table
        public Source Source { get; set; } // Navigation property

        public int? ChapterCount { get; set; } = 0; // Tổng số chapter
        public int? ViewCount { get; set; } = 0; // Tổng số lượt xem
        [NotMapped]
        public int? FavoriteCount { get; set; } = 0; // Tổng số lượt yêu thích, not stored in database
        public DateTime? LastSyncAt { get; set; } = DateTime.UtcNow; // Thời gian đồng bộ gần nhất

        [Display(Name = "ID bên ngoài")]
        public string ExternalId { get; set; } // MangaDex UUID or Imgur Album ID

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Ngày cập nhật")]
        public DateTime UpdatedAt { get; set; }

        [Display(Name = "Nhóm")]
        public int? GroupId { get; set; }
        public Group? Group { get; set; }

        [Display(Name = "Người tải lên")]
        public int? UploadedByUserId { get; set; }
        [ForeignKey("UploadedByUserId")]
        public User? UploadedByUser { get; set; }

        public ICollection<Chapter> Chapters { get; set; }
        public ICollection<MangaGenre> MangaGenres { get; set; }
        public ICollection<Comment> Comments { get; set; }
    }
}