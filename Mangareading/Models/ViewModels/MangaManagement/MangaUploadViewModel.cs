using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Mangareading.Models;

namespace Mangareading.Models.ViewModels
{
    public class MangaUploadViewModel
    {
        [Required(ErrorMessage = "Tên truyện là bắt buộc.")]
        [Display(Name = "Tên truyện")]
        [StringLength(255, ErrorMessage = "Tên truyện không được vượt quá 255 ký tự.")]
        public string Title { get; set; } = null!;

        [Display(Name = "Tên khác")]
        [StringLength(255, ErrorMessage = "Tên khác không được vượt quá 255 ký tự.")]
        public string? AlternativeTitle { get; set; }

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Ảnh bìa")]
        public IFormFile? CoverFile { get; set; }

        [Display(Name = "Tác giả")]
        [Required(ErrorMessage = "Tên tác giả là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Tên tác giả không được vượt quá 100 ký tự.")]
        public string Author { get; set; } = null!;

        [Display(Name = "Họa sỹ")]
        [Required(ErrorMessage = "Tên họa sỹ là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Tên họa sỹ không được vượt quá 100 ký tự.")]
        public string Artist { get; set; } = null!;

        [Display(Name = "Trạng thái")]
        [Required(ErrorMessage = "Trạng thái là bắt buộc.")]
        public string Status { get; set; } = null!;

        [Display(Name = "Năm phát hành")]
        [Range(1900, 2100, ErrorMessage = "Năm phát hành phải nằm trong khoảng từ 1900 đến 2100.")]
        public int? PublicationYear { get; set; }

        [Display(Name = "Thể loại")]
        [Required(ErrorMessage = "Phải chọn ít nhất một thể loại.")]
        public List<int> GenreIds { get; set; } = new();
    }
}


