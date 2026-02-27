using System.ComponentModel.DataAnnotations;

namespace Mangareading.Models
{
    public class ProfileUpdateViewModel
    {
        // Thông tin cơ bản
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;

        [Url(ErrorMessage = "URL ảnh đại diện không hợp lệ")]
        [Display(Name = "URL Ảnh đại diện")]
        public string AvatarUrl { get; set; } = null!;

        // Đổi mật khẩu (optional)
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        [StringLength(100, ErrorMessage = "{0} phải dài ít nhất {2} và tối đa {1} ký tự.", MinimumLength = 6)]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu mới và mật khẩu xác nhận không khớp.")]
        public string? ConfirmPassword { get; set; }
    }
} 