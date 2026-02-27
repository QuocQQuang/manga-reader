using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Mangareading.Models;
// using Mangareading.Models.ViewModels; // REMOVE THIS LINE
using Mangareading.ViewModels; // ADD THIS LINE
using Mangareading.Models.DTOs;
using Mangareading.Repositories;
using Mangareading.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using Microsoft.EntityFrameworkCore;

namespace Mangareading.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("[controller]")]
    public class MangaUploadController : Controller
    {
        private readonly IMangaRepository _mangaRepository;
        private readonly IChapterRepository _chapterRepository;
        private readonly YourDbContext _context;
        private readonly IImgurService _imgurService;
        private readonly ILogger<MangaUploadController> _logger;
        private readonly IUserService _userService;

        private const int MaxImageWidth = 1200; // Max width in pixels for cover
        private const int MaxImageHeight = 1800; // Max height in pixels for cover
        private const int JpegQuality = 85; // Quality for resized JPEGs

        public MangaUploadController(
            IMangaRepository mangaRepository,
            IChapterRepository chapterRepository,
            YourDbContext context,
            IImgurService imgurService,
            ILogger<MangaUploadController> logger,
            IUserService userService)
        {
            _mangaRepository = mangaRepository;
            _chapterRepository = chapterRepository;
            _context = context;
            _imgurService = imgurService;
            _logger = logger;
            _userService = userService;
        }

        // GET: /MangaUpload/CreateManga
        [HttpGet("CreateManga")]
        public IActionResult CreateManga()
        {
            // Get genres for dropdown
            ViewBag.Genres = _context.Genres.OrderBy(g => g.GenreName).ToList();
            return View();
        }

        // GET: /MangaUpload/ManageManga
        [HttpGet("ManageManga")]
        public async Task<IActionResult> ManageManga()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var mangas = await _mangaRepository.GetMangasByUploaderAsync(userId);
            return View(mangas);
        }

        // GET: /MangaUpload/ManagePages/{chapterId}
        [HttpGet("ManagePages/{chapterId}")]
        public async Task<IActionResult> ManagePages(int chapterId)
        {
            var chapter = await _chapterRepository.GetChapterByIdAsync(chapterId);
            if (chapter == null)
            {
                return NotFound();
            }

            var manga = chapter.MangaId.HasValue ? await _mangaRepository.GetByIdAsync(chapter.MangaId.Value) : null;
            if (manga == null)
            {
                return NotFound();
            }

            // Check if user is the uploader
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (manga.UploadedByUserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            ViewBag.Chapter = chapter;
            ViewBag.Manga = manga;

            var pages = await _chapterRepository.GetPagesByChapterIdAsync(chapterId);
            return View(pages);
        }

        // GET: /MangaUpload/EditManga/{id}
        [HttpGet("EditManga/{id}")]
        public async Task<IActionResult> EditManga(int id)
        {
            var manga = await _mangaRepository.GetByIdAsync(id);
            if (manga == null)
            {
                return NotFound();
            }

            // Check ownership or admin role
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (manga.UploadedByUserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid(); // Or RedirectToAction("AccessDenied", "Account");
            }

            var allGenres = await _context.Genres.OrderBy(g => g.GenreName).ToListAsync();
            var chapters = await _chapterRepository.GetChaptersByMangaIdAsync(id);

            var viewModel = new MangaEditViewModel
            {
                Manga = manga,
                AllGenres = allGenres,
                Chapters = chapters.OrderByDescending(c => c.ChapterNumber).ToList() // Order chapters
            };

            return View(viewModel);
        }
    }
}
