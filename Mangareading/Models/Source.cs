using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mangareading.Models
{
    public class Source
    {
        [Key]
        public int SourceId { get; set; }

        [Required(ErrorMessage = "Tên nguồn là bắt buộc.")]
        [Display(Name = "Tên nguồn")]
        public string SourceName { get; set; }

        [Display(Name = "URL nguồn")]
        public string SourceUrl { get; set; }

        [Display(Name = "API Base URL")]
        public string ApiBaseUrl { get; set; }

        [Display(Name = "Hoạt động")]
        public bool IsActive { get; set; }

        public ICollection<Manga> Mangas { get; set; }
    }
}