using System.ComponentModel.DataAnnotations;

namespace Mangareading.Models
{
    public class ReplyDTO
    {
        [Required]
        public string Content { get; set; }
    }
}
