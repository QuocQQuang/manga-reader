using System.Collections.Generic;
using System.Threading.Tasks;
using Mangareading.Models;
using Mangareading.DTOs;

namespace Mangareading.Repositories
{
    public interface IChapterRepository
    {
        Task<List<Chapter>> GetChaptersByMangaIdAsync(int mangaId);
        Task<Chapter> GetChapterByExternalIdAsync(string externalId);
        Task<Chapter> AddOrUpdateChapterAsync(ChapterDto chapterDto, int mangaId, int sourceId);
        Task<List<Page>> GetChapterPagesAsync(int chapterId);
        Task<Chapter> GetChapterByIdAsync(int chapterId);

        Task AddChapterPagesAsync(int chapterId, ChapterReadDto chapterContent);
        Task<bool> ChapterExistsAsync(string externalId);

        // Upload chapter methods
        Task<Chapter> CreateChapterAsync(Chapter chapterData);
        Task<Chapter> UpdateChapterAsync(int chapterId, Chapter chapterData);
        Task DeleteChapterAsync(int chapterId);
        Task UpdateChapterPagesAsync(int chapterId, List<string> orderedImageUrls);
        Task<List<Page>> GetPagesByChapterIdAsync(int chapterId);
    }
}