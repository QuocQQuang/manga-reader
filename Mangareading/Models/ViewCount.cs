using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mangareading.Models
{
    public class ViewCount
    {
        // Khóa chính được cấu hình trong YourDbContext
        /// <summary>
        /// Có trigger trong db thêm vào MangaManga khi thêm xóa lượt xem
        /// </summary>
        public int MangaId { get; set; }
        public Manga Manga { get; set; }
        
        public int ChapterId { get; set; }
        public Chapter Chapter { get; set; }
        
        public int? UserId { get; set; }
        public User User { get; set; }
        
        public string IpAddress { get; set; }
        
        public DateTime ViewedAt { get; set; }
    }
}
