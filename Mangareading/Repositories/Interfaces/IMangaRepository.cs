﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mangareading.Models;
using Mangareading.DTOs;

namespace Mangareading.Repositories
{
    public interface IMangaRepository
    {
        Task<List<Manga>> GetLatestMangasAsync(int count);
        Task<Manga> GetByExternalIdAsync(string externalId);
        Task<Manga> GetByIdAsync(int mangaId);

        Task<Manga> AddOrUpdateMangaAsync(MangaDto mangaDto, int sourceId);
        Task<bool> MangaExistsAsync(string externalId);

        // Upload manga methods
        Task<Manga> CreateMangaAsync(Manga mangaData, List<int> genreIds, int userId);
        Task<Manga> UpdateMangaAsync(int mangaId, Manga mangaData, List<int> genreIds, string? newCoverUrl);
        Task<List<Manga>> GetMangasByUploaderAsync(int userId);
        Task DeleteMangaAsync(int mangaId);

        // Favorite manga methods
        //Task<bool> AddFavoriteAsync(int userId, int mangaId);
        //Task<bool> RemoveFavoriteAsync(int userId, int mangaId);
        //Task<bool> IsFavoritedAsync(int userId, int mangaId);
        //Task<List<Manga>> GetUserFavoritesAsync(int userId);
    }
}
