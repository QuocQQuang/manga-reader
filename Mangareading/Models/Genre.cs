using System;
using System.Collections.Generic;

namespace Mangareading.Models
{
    public class Genre
    {
        public int GenreId { get; set; }
        public string GenreName { get; set; }

        public ICollection<MangaGenre> MangaGenres { get; set; }
    }
}