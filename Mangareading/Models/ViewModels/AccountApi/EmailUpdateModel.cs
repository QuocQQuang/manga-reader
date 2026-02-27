using System.ComponentModel.DataAnnotations;

namespace Mangareading.Models.ViewModels.AccountApi
{
    public class EmailUpdateModel
    {
        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
        [Display(Name = "Email mới")]
        public string NewEmail { get; set; }
    }
} 