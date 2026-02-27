using Mangareading.Models;
using System.Collections.Generic;

namespace Mangareading.Models.ViewModelsViewModels
{
    public class MangaEditViewModel
    {
        public Manga Manga { get; set; } = null!;
        public List<Genre> AllGenres { get; set; } = null!;
        public List<Chapter> Chapters { get; set; } = null!;

    }
}
