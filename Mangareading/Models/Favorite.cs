using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mangareading.Models
{
    public class Favorite
    {
        // Có trigger trong db thêm vào MangaManga khi thêm xóa Favourite

        // Không sử dụng FavoriteId vì trong OnModelCreating đã định nghĩa composite key
        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; }
        
        public int MangaId { get; set; }
        public Manga Manga { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }
}