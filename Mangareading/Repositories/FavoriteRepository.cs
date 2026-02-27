using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mangareading.Models;

namespace Mangareading.Repositories
{
    public interface IFavoriteRepository
    {
        Task AddFavoriteAsync(int userId, int mangaId);
        Task RemoveFavoriteAsync(int userId, int mangaId);
        Task<bool> IsFavoriteAsync(int userId, int mangaId);
        Task<int> GetFavoriteCountAsync(int mangaId);
        Task<List<Manga>> GetUserFavoritesAsync(int userId);
    }

    public class FavoriteRepository : IFavoriteRepository
    {
        private readonly YourDbContext _context;

        public FavoriteRepository(YourDbContext context)
        {
            _context = context;
        }

        public async Task AddFavoriteAsync(int userId, int mangaId)
        {
            // Kiểm tra xem đã yêu thích chưa
            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.MangaId == mangaId);

            if (existingFavorite == null)
            {
                // Thêm vào danh sách yêu thích
                var favorite = new Favorite
                {
                    UserId = userId,
                    MangaId = mangaId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Favorites.Add(favorite);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveFavoriteAsync(int userId, int mangaId)
        {
            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.MangaId == mangaId);

            if (favorite != null)
            {
                // Use raw SQL to delete the favorite without using OUTPUT clause
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM Favorites WHERE UserId = {0} AND MangaId = {1}",
                    userId, mangaId);
            }
        }

        public async Task<bool> IsFavoriteAsync(int userId, int mangaId)
        {
            return await _context.Favorites
                .AnyAsync(f => f.UserId == userId && f.MangaId == mangaId);
        }

        public async Task<int> GetFavoriteCountAsync(int mangaId)
        {
            return await _context.Favorites
                .Where(f => f.MangaId == mangaId)
                .CountAsync();
        }

        public async Task<List<Manga>> GetUserFavoritesAsync(int userId)
        {
            return await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Manga)
                .Select(f => f.Manga)
                .ToListAsync();
        }
    }
}
