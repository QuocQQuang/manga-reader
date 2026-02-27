using System.ComponentModel.DataAnnotations;

namespace Mangareading.Models
{
    public class ApiCache
    {
        [Key]
        public string CacheKey { get; set; }
        public string CacheData { get; set; }
        public DateTime ExpireAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}