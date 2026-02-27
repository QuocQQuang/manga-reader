namespace Mangareading.Models
{
    public class MangaGroup
    {
        public int MangaId { get; set; }
        public Manga Manga { get; set; }

        public int GroupId { get; set; }
        public Group Group { get; set; }
    }
}