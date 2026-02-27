using Mangareading.Models;
using System.Collections.Generic;

namespace Mangareading.ViewModels
{
    public class MangaEditViewModel
    {
        public Manga Manga { get; set; }
        public List<Genre> AllGenres { get; set; }
        public List<Chapter> Chapters { get; set; }

    
    }
}
