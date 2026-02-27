using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Mangareading.DTOs
{
    public class MangaUpdateDto
    {
        [Required(ErrorMessage = "Tên truyện là bắt buộc.")]
        [StringLength(255)]
        public string Title { get; set; }

        [StringLength(255)]
        public string AlternativeTitle { get; set; }

        public string Description { get; set; }

        [Required(ErrorMessage = "Tên tác giả là bắt buộc.")]
        [StringLength(100)]
        public string Author { get; set; }

        [Required(ErrorMessage = "Tên họa sĩ là bắt buộc.")]
        [StringLength(100)]
        public string Artist { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc.")]
        [StringLength(50)]
        public string Status { get; set; }

        public int? PublicationYear { get; set; }

        public List<int> GenreIds { get; set; } = new List<int>();

        public IFormFile CoverFile { get; set; } // For uploading a new cover
    }
}
