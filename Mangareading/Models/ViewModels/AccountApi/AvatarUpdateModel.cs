using System.ComponentModel.DataAnnotations;

namespace Mangareading.Models.ViewModels.AccountApi
{
    public class AvatarUpdateModel
    {
        [Url(ErrorMessage = "URL ảnh đại diện không hợp lệ.")]
        [Display(Name = "URL Ảnh đại diện mới")]
        public string? NewAvatarUrl { get; set; } // Allow null or empty string to clear avatar
    }
} 