using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mangareading.Repositories.Interfaces;
using Mangareading.Models;
using Mangareading.DTOs;

namespace Mangareading.Repositories
{
    public class MangaRepository : IMangaRepository
    {
        private readonly YourDbContext _context;
        private readonly ILogger<MangaRepository> _logger;

        public MangaRepository(
            YourDbContext context,
            ILogger<MangaRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Manga>> GetLatestMangasAsync(int count)
        {
            return await _context.Mangas
                .OrderByDescending(m => m.UpdatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<Manga> GetByExternalIdAsync(string externalId)
        {
            return await _context.Mangas
                .FirstOrDefaultAsync(m => m.ExternalId == externalId);
        }

        public async Task<bool> MangaExistsAsync(string externalId)
        {
            return await _context.Mangas.AnyAsync(m => m.ExternalId == externalId);
        }

        public async Task<Manga> AddOrUpdateMangaAsync(MangaDto mangaDto, int sourceId)
        {
            try
            {
                var existingManga = await _context.Mangas
                    .Include(m => m.MangaGenres)
                    .ThenInclude(mg => mg.Genre)
                    .FirstOrDefaultAsync(m => m.ExternalId == mangaDto.Id);

                if (existingManga == null)
                {
                    // Tạo manga mới
                    existingManga = CreateMangaFromDto(mangaDto, sourceId);
                    _context.Mangas.Add(existingManga);
                }
                else
                {
                    // Cập nhật manga hiện có
                    UpdateMangaFromDto(existingManga, mangaDto);
                }

                await _context.SaveChangesAsync();

                // Cập nhật ngày tạo và ngày cập nhật dựa trên chapters
                await UpdateMangaDatesFromChaptersAsync(existingManga.MangaId);

                return existingManga;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving manga {mangaDto.Id}");
                throw;
            }
        }

        private Manga CreateMangaFromDto(MangaDto dto, int sourceId)
        {
            var coverRelationship = dto.Relationships?.FirstOrDefault(r => r.Type == "cover_art");
            string coverFileName = null;

            if (coverRelationship?.Attributes != null)
            {
                var coverAttributes = System.Text.Json.JsonSerializer.Deserialize<CoverAttributes>(
                    coverRelationship.Attributes.ToString());
                coverFileName = coverAttributes?.FileName;
            }

            string coverUrl = !string.IsNullOrEmpty(coverFileName)
                ? $"https://uploads.mangadex.org/covers/{dto.Id}/{coverFileName}"
                : null;

            // Lấy tên tác giả và họa sĩ
            string author = null;
            string artist = null;

            foreach (var rel in dto.Relationships ?? new List<Relationship>())
            {
                if (rel.Type == "author" && rel.Attributes != null)
                {
                    var authorAttributes = System.Text.Json.JsonSerializer.Deserialize<AuthorAttributes>(
                        rel.Attributes.ToString());
                    author = authorAttributes?.Name;
                }
                else if (rel.Type == "artist" && rel.Attributes != null)
                {
                    var artistAttributes = System.Text.Json.JsonSerializer.Deserialize<AuthorAttributes>(
                        rel.Attributes.ToString());
                    artist = artistAttributes?.Name;
                }
            }

            // Lấy tiêu đề và mô tả ưu tiên tiếng Việt và tiếng Anh
            string title = GetBestTitle(dto.Attributes.Title);
            string altTitle = GetAltTitle(dto.Attributes.AltTitles);
            string description = GetBestDescription(dto.Attributes.Description);

            // Xử lý tags/genres
            var genres = ProcessGenres(dto.Attributes.Tags);

            var manga = new Manga
            {
                Title = title,
                AlternativeTitle = altTitle,
                Description = description,
                CoverUrl = coverUrl,
                Author = author,
                Artist = artist,
                Status = dto.Attributes.Status,
                PublicationYear = dto.Attributes.Year,
                OriginalLanguage = dto.Attributes.OriginalLanguage,
                SourceId = sourceId,
                ExternalId = dto.Id,
                CreatedAt = DateTime.Now, // Sẽ cập nhật lại sau khi lấy chapter
                UpdatedAt = DateTime.Now, // Sẽ cập nhật lại sau khi lấy chapter
                MangaGenres = new List<MangaGenre>()
            };

            // Thêm genres
            foreach (var genreName in genres)
            {
                var genre = GetOrCreateGenre(genreName);
                manga.MangaGenres.Add(new MangaGenre
                {
                    Manga = manga,
                    Genre = genre
                });
            }

            return manga;
        }

        private void UpdateMangaFromDto(Manga manga, MangaDto dto)
        {
            if (dto == null || manga == null)
                return;

            // Cập nhật thông tin cơ bản
            if (dto.Attributes?.Title != null)
            {
                manga.Title = GetBestTitle(dto.Attributes.Title);
            }

            if (dto.Attributes?.AltTitles != null)
            {
                manga.AlternativeTitle = GetAltTitle(dto.Attributes.AltTitles);
            }

            if (dto.Attributes?.Description != null)
            {
                manga.Description = GetBestDescription(dto.Attributes.Description);
            }

            if (dto.Attributes?.Status != null)
            {
                manga.Status = dto.Attributes.Status;
            }

            if (dto.Attributes?.Year.HasValue == true)
            {
                manga.PublicationYear = dto.Attributes.Year;
            }

            // Cập nhật ngôn ngữ gốc
            if (!string.IsNullOrEmpty(dto.Attributes?.OriginalLanguage))
            {
                manga.OriginalLanguage = dto.Attributes.OriginalLanguage;
            }

            // Cập nhật thời gian
            manga.UpdatedAt = DateTime.Now;

            // Cập nhật cover
            var coverRelationship = dto.Relationships?.FirstOrDefault(r => r.Type == "cover_art");
            if (coverRelationship?.Attributes != null)
            {
                var coverAttributes = System.Text.Json.JsonSerializer.Deserialize<CoverAttributes>(
                    coverRelationship.Attributes.ToString());
                var coverFileName = coverAttributes?.FileName;

                if (!string.IsNullOrEmpty(coverFileName))
                {
                    manga.CoverUrl = $"https://uploads.mangadex.org/covers/{dto.Id}/{coverFileName}";
                }
            }

            // Cập nhật tác giả và họa sĩ
            foreach (var rel in dto.Relationships ?? new List<Relationship>())
            {
                if (rel.Type == "author" && rel.Attributes != null)
                {
                    var authorAttributes = System.Text.Json.JsonSerializer.Deserialize<AuthorAttributes>(
                        rel.Attributes.ToString());
                    if (!string.IsNullOrEmpty(authorAttributes?.Name))
                        manga.Author = authorAttributes.Name;
                }
                else if (rel.Type == "artist" && rel.Attributes != null)
                {
                    var artistAttributes = System.Text.Json.JsonSerializer.Deserialize<AuthorAttributes>(
                        rel.Attributes.ToString());
                    if (!string.IsNullOrEmpty(artistAttributes?.Name))
                        manga.Artist = artistAttributes.Name;
                }
            }

            // Cập nhật genres/tags
            if (dto.Attributes?.Tags != null)
            {
                var genres = ProcessGenres(dto.Attributes.Tags);

                // Xóa genres cũ
                var genresToRemove = manga.MangaGenres.ToList();
                foreach (var mg in genresToRemove)
                {
                    manga.MangaGenres.Remove(mg);
                }

                // Thêm genres mới
                foreach (var genreName in genres)
                {
                    var genre = GetOrCreateGenre(genreName);
                    manga.MangaGenres.Add(new MangaGenre
                    {
                        Manga = manga,
                        Genre = genre
                    });
                }
            }
        }

        private Genre GetOrCreateGenre(string name)
        {
            var genre = _context.Genres.FirstOrDefault(g => g.GenreName == name);

            if (genre == null)
            {
                genre = new Genre { GenreName = name };
                _context.Genres.Add(genre);
            }

            return genre;
        }

        private string GetBestTitle(Dictionary<string, string> titles)
        {
            if (titles == null || titles.Count == 0)
                return null;

            // Ưu tiên tiếng Việt
            if (titles.TryGetValue("vi", out string viTitle))
                return viTitle;

            // Tiếng Anh
            if (titles.TryGetValue("en", out string enTitle))
                return enTitle;

            // Fallback: Ngôn ngữ bất kỳ
            return titles.FirstOrDefault().Value;
        }

        private string GetAltTitle(List<Dictionary<string, string>> altTitles)
        {
            if (altTitles == null || altTitles.Count == 0)
                return null;

            var titles = new List<string>();

            // Thu thập các tiêu đề thay thế từ nhiều ngôn ngữ
            foreach (var titleDict in altTitles)
            {
                if (titleDict.TryGetValue("vi", out string viTitle))
                    titles.Add(viTitle);
                else if (titleDict.TryGetValue("en", out string enTitle))
                    titles.Add(enTitle);
                else if (titleDict.Any())
                    titles.Add(titleDict.FirstOrDefault().Value);
            }

            // Ghép thành một chuỗi duy nhất
            return titles.Count > 0 ? string.Join(" | ", titles.Distinct().Take(3)) : null;
        }

        private string GetBestDescription(Dictionary<string, string> descriptions)
        {
            if (descriptions == null || descriptions.Count == 0)
                return null;

            // Ưu tiên tiếng Việt
            if (descriptions.TryGetValue("vi", out string viDesc) && !string.IsNullOrWhiteSpace(viDesc))
                return viDesc;

            // Tiếng Anh
            if (descriptions.TryGetValue("en", out string enDesc) && !string.IsNullOrWhiteSpace(enDesc))
                return enDesc;

            // Fallback: Ngôn ngữ bất kỳ
            return descriptions.FirstOrDefault().Value;
        }

        private List<string> ProcessGenres(List<TagDto> tags)
        {
            var result = new List<string>();

            if (tags == null)
                return result;

            foreach (var tag in tags)
            {
                var tagName = tag.Attributes?.Name?.FirstOrDefault().Value ?? "";
                if (!string.IsNullOrWhiteSpace(tagName))
                {
                    result.Add(tagName);
                }
            }

            return result;
        }
        public async Task<Manga> GetByIdAsync(int mangaId)
        {
            return await _context.Mangas
                .Include(m => m.MangaGenres)
                .ThenInclude(mg => mg.Genre)
                .FirstOrDefaultAsync(m => m.MangaId == mangaId);
        }

        private async Task UpdateMangaDatesFromChaptersAsync(int mangaId)
        {
            try
            {
                var manga = await _context.Mangas.FindAsync(mangaId);
                if (manga == null)
                {
                    _logger.LogWarning($"No manga found with ID {mangaId} when updating dates");
                    return;
                }

                var chapters = await _context.Chapters
                    .Where(c => c.MangaId == mangaId)
                    .OrderBy(c => c.UploadDate)
                    .ToListAsync();

                if (chapters != null && chapters.Any())
                {
                    // Lấy ngày của chapter đầu tiên (cũ nhất) làm ngày tạo manga
                    manga.CreatedAt = chapters.First().UploadDate;

                    // Lấy ngày của chapter mới nhất làm ngày cập nhật manga
                    manga.UpdatedAt = chapters.OrderByDescending(c => c.UploadDate).First().UploadDate;

                    _logger.LogInformation($"Updated manga dates for manga ID {mangaId}. Created: {manga.CreatedAt}, Updated: {manga.UpdatedAt}");
                }
                else
                {
                    // No chapters available, use current time but don't set far-future date
                    // Only update if current values are suspect
                    if (manga.UpdatedAt > DateTime.Now.AddDays(1) || manga.CreatedAt > DateTime.Now.AddDays(1))
                    {
                        _logger.LogWarning($"Manga {mangaId} has future dates, resetting to current date");
                        manga.CreatedAt = DateTime.Now;
                        manga.UpdatedAt = DateTime.Now;
                    }
                    _logger.LogInformation($"No chapters found for manga {mangaId}, keeping existing dates");
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating manga dates for manga ID {mangaId}");
            }
        }

        // Upload manga methods implementation
        public async Task<Manga> CreateMangaAsync(Manga mangaData, List<int> genreIds, int userId)
        {
            try
            {
                // Set upload user
                mangaData.UploadedByUserId = userId;

                // Set source to local (Imgur)
                var imgurSource = await _context.Sources.FirstOrDefaultAsync(s => s.SourceName == "Imgur");
                if (imgurSource == null)
                {
                    // Create Imgur source if it doesn't exist
                    imgurSource = new Source
                    {
                        SourceName = "Imgur",
                        SourceUrl = "https://imgur.com",
                        ApiBaseUrl = "https://api.imgur.com",
                        IsActive = true
                    };
                    _context.Sources.Add(imgurSource);
                    await _context.SaveChangesAsync();
                }
                mangaData.SourceId = imgurSource.SourceId;

                // Set dates
                mangaData.CreatedAt = DateTime.Now;
                mangaData.UpdatedAt = DateTime.Now;
                mangaData.LastSyncAt = DateTime.Now;

                // Set default values
                mangaData.ChapterCount = 0;
                mangaData.ViewCount = 0;

                // Generate a unique ExternalId for Imgur uploads
                mangaData.ExternalId = $"imgur-{Guid.NewGuid()}";

                // Add manga to database
                _context.Mangas.Add(mangaData);
                await _context.SaveChangesAsync();

                // Add genres
                if (genreIds != null && genreIds.Any())
                {
                    foreach (var genreId in genreIds)
                    {
                        var genre = await _context.Genres.FindAsync(genreId);
                        if (genre != null)
                        {
                            _context.MangaGenres.Add(new MangaGenre
                            {
                                MangaId = mangaData.MangaId,
                                GenreId = genreId
                            });
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                return mangaData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating manga: {mangaData.Title}");
                throw;
            }
        }

        public async Task<Manga> UpdateMangaAsync(int mangaId, Manga mangaData, List<int> genreIds, string? newCoverUrl)
        {
            try
            {
                var existingManga = await _context.Mangas
                    .Include(m => m.MangaGenres)
                    .FirstOrDefaultAsync(m => m.MangaId == mangaId);

                if (existingManga == null)
                {
                    throw new KeyNotFoundException($"Manga with ID {mangaId} not found");
                }

                // Update basic properties
                existingManga.Title = mangaData.Title;
                existingManga.AlternativeTitle = mangaData.AlternativeTitle;
                existingManga.Description = mangaData.Description;
                existingManga.Author = mangaData.Author;
                existingManga.Artist = mangaData.Artist;
                existingManga.Status = mangaData.Status;
                existingManga.PublicationYear = mangaData.PublicationYear;
                existingManga.OriginalLanguage = mangaData.OriginalLanguage;
                existingManga.GroupId = mangaData.GroupId;

                // Update cover URL if provided
                if (!string.IsNullOrEmpty(newCoverUrl))
                {
                    existingManga.CoverUrl = newCoverUrl;
                }

                // Update timestamp
                existingManga.UpdatedAt = DateTime.Now;

                // Update genres
                if (genreIds != null)
                {
                    // Remove existing genres
                    _context.MangaGenres.RemoveRange(existingManga.MangaGenres);

                    // Add new genres
                    foreach (var genreId in genreIds)
                    {
                        _context.MangaGenres.Add(new MangaGenre
                        {
                            MangaId = existingManga.MangaId,
                            GenreId = genreId
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return existingManga;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating manga with ID {mangaId}");
                throw;
            }
        }

        public async Task<List<Manga>> GetMangasByUploaderAsync(int userId)
        {
            try
            {
                return await _context.Mangas
                    .Where(m => m.UploadedByUserId == userId)
                    .Include(m => m.MangaGenres)
                    .ThenInclude(mg => mg.Genre)
                    .OrderByDescending(m => m.UpdatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting mangas for uploader {userId}");
                return new List<Manga>();
            }
        }

        public async Task DeleteMangaAsync(int mangaId)
        {
            try
            {
                // First check if manga exists
                var mangaExists = await _context.Mangas.AnyAsync(m => m.MangaId == mangaId);
                if (!mangaExists)
                {
                    _logger.LogWarning($"Manga with ID {mangaId} not found during deletion");
                    throw new KeyNotFoundException($"Manga with ID {mangaId} not found");
                }

                // Use execution strategy with transaction to handle retries correctly
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    // Begin transaction within execution strategy
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        // 1. Delete reading histories
                        var historiesDeleted = await _context.Database
                            .ExecuteSqlRawAsync("DELETE FROM ReadingHistories WHERE MangaId = {0}", mangaId);
                        _logger.LogInformation($"Deleted {historiesDeleted} reading history records for manga {mangaId}");

                        // 2. Delete favorites
                        var favoritesDeleted = await _context.Database
                            .ExecuteSqlRawAsync("DELETE FROM Favorites WHERE MangaId = {0}", mangaId);
                        _logger.LogInformation($"Deleted {favoritesDeleted} favorite records for manga {mangaId}");

                        // 3. Delete comments
                        var commentsDeleted = await _context.Database
                            .ExecuteSqlRawAsync("DELETE FROM Comments WHERE MangaId = {0}", mangaId);
                        _logger.LogInformation($"Deleted {commentsDeleted} comment records for manga {mangaId}");

                        // 4. Delete manga genres
                        var genresDeleted = await _context.Database
                            .ExecuteSqlRawAsync("DELETE FROM MangaGenres WHERE MangaId = {0}", mangaId);
                        _logger.LogInformation($"Deleted {genresDeleted} manga genre associations for manga {mangaId}");

                        // 5. Get all chapters for this manga to delete their related data
                        var chapters = await _context.Chapters
                            .Where(c => c.MangaId == mangaId)
                            .Select(c => c.ChapterId)
                            .ToListAsync();

                        foreach (var chapterId in chapters)
                        {
                            // 5a. Delete view counts for each chapter
                            var viewCountsDeleted = await _context.Database
                                .ExecuteSqlRawAsync("DELETE FROM ViewCounts WHERE ChapterId = {0}", chapterId);
                            _logger.LogInformation($"Deleted {viewCountsDeleted} view count records for chapter {chapterId}");

                            // 5b. Delete pages for each chapter
                            var pagesDeleted = await _context.Database
                                .ExecuteSqlRawAsync("DELETE FROM Pages WHERE ChapterId = {0}", chapterId);
                            _logger.LogInformation($"Deleted {pagesDeleted} page records for chapter {chapterId}");
                        }

                        // 6. Delete all chapters for this manga
                        var chaptersDeleted = await _context.Database
                            .ExecuteSqlRawAsync("DELETE FROM Chapters WHERE MangaId = {0}", mangaId);
                        _logger.LogInformation($"Deleted {chaptersDeleted} chapter records for manga {mangaId}");

                        // 7. Finally delete the manga itself
                        var mangaDeleted = await _context.Database
                            .ExecuteSqlRawAsync("DELETE FROM Mangas WHERE MangaId = {0}", mangaId);
                        _logger.LogInformation($"Deleted manga with ID {mangaId}: {mangaDeleted} record affected");

                        if (mangaDeleted == 0)
                        {
                            throw new DbUpdateConcurrencyException("The manga was not found when attempting to delete it.");
                        }

                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, $"Transaction rolled back for manga deletion {mangaId}");
                        throw;
                    }
                });
            }
            catch (KeyNotFoundException)
            {
                // Re-throw key not found exception
                throw;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, $"Concurrency exception when deleting manga {mangaId}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting manga with ID {mangaId}");
                throw;
            }
        }
    }
}
