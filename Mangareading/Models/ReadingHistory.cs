using System.ComponentModel.DataAnnotations;

namespace Mangareading.Models
{
    public class ReadingHistory
    {
        [Key]
        public int HistoryId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }

        public int MangaId { get; set; }
        public Manga Manga { get; set; }

        public int ChapterId { get; set; }
        public Chapter Chapter { get; set; }

        public DateTime ReadAt { get; set; }
    }
}