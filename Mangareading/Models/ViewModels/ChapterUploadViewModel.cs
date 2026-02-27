using System.ComponentModel.DataAnnotations;

namespace Mangareading.Models.ViewModels
{
    public class ChapterUploadViewModel
    {
        [Required(ErrorMessage = "ID truyện là bắt buộc.")]
        public int MangaId { get; set; }

        [Display(Name = "Tiêu đề chapter")]
        [StringLength(255, ErrorMessage = "Tiêu đề chapter không được vượt quá 255 ký tự.")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Số chapter là bắt buộc.")]
        [Display(Name = "Số chapter")]
        [Range(0, 9999.99, ErrorMessage = "Số chapter phải nằm trong khoảng từ 0 đến 9999.99.")]
        public decimal ChapterNumber { get; set; }

        [Display(Name = "Ngôn ngữ")]
        [StringLength(10, ErrorMessage = "Mã ngôn ngữ không được vượt quá 10 ký tự.")]
        public string? LanguageCode { get; set; } = "vi";
    }
}
