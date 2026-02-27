namespace Mangareading.Models
{
    public class ReadingProgress
    {
        public int UserId { get; set; }
        public User User { get; set; }

        public int MangaId { get; set; }
        public Manga Manga { get; set; }

        public int ChapterId { get; set; }
        public Chapter Chapter { get; set; }

        public int PageNumber { get; set; }
        public DateTime LastReadAt { get; set; }
    }
}