using System.Collections.Generic;
using System.Threading.Tasks;
using Mangareading.Models;

namespace Mangareading.Services.Interfaces
{
    public interface IMangaService
    {
        Task<List<Manga>> GetLatestMangasAsync(int count = 10);
        Task<Manga> GetMangaByIdAsync(int id);
        Task SyncLatestVietnameseMangaAsync(int count = 10);
        Task<Manga> GetByMangadexIdAsync(string mangadexId);
    }
}