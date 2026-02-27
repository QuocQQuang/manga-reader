using System.Collections.Generic;

namespace Mangareading.Models.ViewModels
{
    public class ChapterViewModel
    {
        public Chapter Chapter { get; set; }
        public string BaseUrl { get; set; }
        public string Hash { get; set; }
        public List<string> Images { get; set; }
        public List<string> DataSaver { get; set; } 
        public int CurrentPage { get; set; }
        public Chapter PreviousChapter { get; set; }
        public Chapter NextChapter { get; set; }

        public int TotalPages => Images?.Count ?? 0;

        public string GetImageUrl(int index, bool dataSaver = false)
        {
            if (index < 0 || (dataSaver && (DataSaver == null || index >= DataSaver.Count))
                || (!dataSaver && (Images == null || index >= Images.Count)))
            {
                return "/images/image-not-found.png";
            }

            if (dataSaver)
            {
                return $"{BaseUrl}/data-saver/{Hash}/{DataSaver[index]}";
            }

            return $"{BaseUrl}/data/{Hash}/{Images[index]}";
        }

        // Phương thức tiện ích để điều hướng trang
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public int PreviousPageNumber => CurrentPage - 1;
        public int NextPageNumber => CurrentPage + 1;
    }
}