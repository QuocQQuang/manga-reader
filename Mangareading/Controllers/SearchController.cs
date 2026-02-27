using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mangareading.Models;
using Mangareading.Repositories;

namespace Mangareading.Controllers
{
    [Route("search")]
    public class SearchController : Controller
    {
        private readonly ILogger<SearchController> _logger;
        private readonly IMangaRepository _mangaRepository;
        private readonly IChapterRepository _chapterRepository;
        private readonly YourDbContext _context;

        public SearchController(
            ILogger<SearchController> logger,
            IMangaRepository mangaRepository,
            IChapterRepository chapterRepository,
            YourDbContext context)
        {
            _logger = logger;
            _mangaRepository = mangaRepository;
            _chapterRepository = chapterRepository;
            _context = context;
        }

        // Redirect basic search to advanced
        [HttpGet("")]
        public IActionResult Index()
        {
            return RedirectToAction("Advanced");
        }

        // Advanced search page
        [HttpGet("advanced")]
        public async Task<IActionResult> Advanced()
        {
            // Get all genres for the filter dropdown
            var genres = await _context.Genres.OrderBy(g => g.GenreName).ToListAsync();
            ViewBag.Genres = genres;
            
            // Get all original languages
            var languages = await _context.Mangas
                .Where(m => m.OriginalLanguage != null && m.OriginalLanguage != "")
                .Select(m => m.OriginalLanguage)
                .Distinct()
                .OrderBy(l => l)
                .ToListAsync();
            
            ViewBag.Languages = languages ?? new List<string?>();
            
            return View();
        }

        // Enhanced autocomplete API for real-time search
        [HttpGet("autocomplete")]
        public async Task<IActionResult> AutoComplete(string term)
        {
            if (string.IsNullOrEmpty(term))
                return Json(new List<object>());

            var lowerTerm = term.ToLower(); // Normalize search term to lower case

            var mangas = await _context.Mangas
                .Where(m => m.Title.ToLower().Contains(lowerTerm) || 
                           (m.AlternativeTitle != null && m.AlternativeTitle.ToLower().Contains(lowerTerm)) ||
                           (m.Author != null && m.Author.ToLower().Contains(lowerTerm)) ||
                           (m.Artist != null && m.Artist.ToLower().Contains(lowerTerm))) // ADDED Artist search
                .OrderBy(m => m.Title)
                .Take(10)
                .Select(m => new { 
                    value = m.Title, // Return Title for display
                    id = m.MangaId,
                    // Optional: Return more info if needed by the dropdown UI
                    author = m.Author,
                    artist = m.Artist, // ADDED Artist to result if needed
                    coverUrl = m.CoverUrl,
                    alternativeTitle = m.AlternativeTitle
                })
                .ToListAsync();

            // Consider adding distinct results if necessary, although less likely with Take(10)

            return Json(mangas);
        }

        // API for search results
        [HttpGet("results")]
        public async Task<IActionResult> Results(
            string query = "",
            string sortBy = "latest", 
            List<int>? genreIds = null,
            List<int>? excludeGenreIds = null,
            string status = "",
            int minChapters = 0,
            int publicationYear = 0,
            string originalLanguage = "",
            DateTime? lastUpdatedAfter = null,
            int page = 1)
        {
            int pageSize = 20;
            
            // Base query
            var mangaQuery = _context.Mangas.AsQueryable();
            
            // Apply search filter - enhanced to search in alternative titles and author names
            if (!string.IsNullOrEmpty(query))
            {
                mangaQuery = mangaQuery.Where(m => 
                    m.Title.Contains(query) || 
                    (m.AlternativeTitle != null && m.AlternativeTitle.Contains(query)) ||
                    (m.Author != null && m.Author.Contains(query)) ||
                    (m.Artist != null && m.Artist.Contains(query)) ||
                    (m.Description != null && m.Description.Contains(query)));
            }
            
            // Apply included genre filters (genres marked with checkmark)
            if (genreIds != null && genreIds.Any())
            {
                foreach (var genreId in genreIds)
                {
                    mangaQuery = mangaQuery.Where(m => 
                        m.MangaGenres.Any(mg => mg.GenreId == genreId));
                }
            }
            
            // Apply excluded genre filters (genres marked with X)
            if (excludeGenreIds != null && excludeGenreIds.Any())
            {
                foreach (var genreId in excludeGenreIds)
                {
                    mangaQuery = mangaQuery.Where(m => 
                        !m.MangaGenres.Any(mg => mg.GenreId == genreId));
                }
            }
            
            // Apply status filter
            if (!string.IsNullOrEmpty(status))
            {
                mangaQuery = mangaQuery.Where(m => m.Status == status);
            }
            
            // Apply minimum chapters filter
            if (minChapters > 0)
            {
                mangaQuery = mangaQuery.Where(m => m.ChapterCount >= minChapters);
            }
            
            // Apply publication year filter
            if (publicationYear > 0)
            {
                mangaQuery = mangaQuery.Where(m => m.PublicationYear == publicationYear);
            }
            
            // Apply original language filter
            if (!string.IsNullOrEmpty(originalLanguage))
            {
                mangaQuery = mangaQuery.Where(m => m.OriginalLanguage == originalLanguage);
            }
            
            // Apply last updated filter
            if (lastUpdatedAfter.HasValue)
            {
                mangaQuery = mangaQuery.Where(m => m.UpdatedAt >= lastUpdatedAfter.Value);
            }
            
            // Apply sorting
            switch (sortBy)
            {
                case "title":
                    mangaQuery = mangaQuery.OrderBy(m => m.Title);
                    break;
                case "popularity":
                    // Since there's no Views property, we'll sort by CreatedAt as a substitute
                    // You may want to add a ViewCount property to your model later
                    mangaQuery = mangaQuery.OrderByDescending(m => m.CreatedAt);
                    break;
                case "oldest":
                    mangaQuery = mangaQuery.OrderBy(m => m.UpdatedAt);
                    break;
                case "latest":
                default:
                    mangaQuery = mangaQuery.OrderByDescending(m => m.UpdatedAt);
                    break;
            }

            // Execute query with pagination
            var totalCount = await mangaQuery.CountAsync();
            var mangas = await mangaQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(m => m.MangaGenres)
                .ThenInclude(mg => mg.Genre)
                .ToListAsync();
            
            // Prepare view data
            ViewBag.Query = query;
            ViewBag.SortBy = sortBy;
            ViewBag.GenreIds = genreIds;
            ViewBag.ExcludeGenreIds = excludeGenreIds;
            ViewBag.Status = status;
            ViewBag.MinChapters = minChapters;
            ViewBag.PublicationYear = publicationYear;
            ViewBag.OriginalLanguage = originalLanguage;
            ViewBag.LastUpdatedAfter = lastUpdatedAfter;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            
            // Load genres for filter dropdown
            ViewBag.Genres = await _context.Genres.OrderBy(g => g.GenreName).ToListAsync();
            
            return View(mangas);
        }
    }
}
