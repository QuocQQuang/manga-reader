using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Mangareading.Models;
using Mangareading.Models.ViewModels;
using Mangareading.Models.DTOs;
using Mangareading.Repositories;
using Mangareading.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using Mangareading.ViewModels;
using Mangareading.DTOs;

namespace Mangareading.Controllers.Api
{
    [Route("api/manga-upload")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class MangaUploadController : ControllerBase
    {
        private readonly IMangaRepository _mangaRepository;
        private readonly IChapterRepository _chapterRepository;
        private readonly YourDbContext _context;
        private readonly IImgurService _imgurService;
        private readonly ILogger<MangaUploadController> _logger;
        private readonly IUserService _userService;
        
        private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private static readonly string[] AllowedMimeTypes = { "image/jpeg", "image/png", "image/gif" };
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
        
        // POST: /api/manga-upload/create
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateManga([FromForm] MangaUploadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            try
            {
                // Upload cover image to Imgur
                string coverUrl = null;
                if (model.CoverFile != null && model.CoverFile.Length > 0)
                {
                    if (model.CoverFile.Length > MaxFileSize)
                    {
                        return BadRequest(new { message = $"Kích thước file quá lớn (tối đa {MaxFileSize / 1024 / 1024} MB)." });
                    }
                    
                    var fileExtension = Path.GetExtension(model.CoverFile.FileName).ToLowerInvariant();
                    var mimeType = model.CoverFile.ContentType.ToLowerInvariant();
                    
                    if (string.IsNullOrEmpty(fileExtension) || !AllowedExtensions.Contains(fileExtension) || !AllowedMimeTypes.Contains(mimeType))
                    {
                        return BadRequest(new { message = "Định dạng file không hợp lệ. Chỉ chấp nhận JPG, PNG, GIF." });
                    }
                    
                    // Process image with ImageSharp
                    using var inputStream = model.CoverFile.OpenReadStream();
                    using var image = await Image.LoadAsync(inputStream);
                    
                    bool resized = false;
                    if (image.Width > MaxImageWidth || image.Height > MaxImageHeight)
                    {
                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(MaxImageWidth, MaxImageHeight),
                            Mode = ResizeMode.Max // Maintain aspect ratio, fit within bounds
                        }));
                        resized = true;
                    }
                    
                    // Save the (potentially resized) image to a new stream
                    using var outputStream = new MemoryStream();
                    string outputFileName = Path.ChangeExtension(model.CoverFile.FileName, ".jpg");
                    await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = JpegQuality });
                    outputStream.Position = 0; // Reset stream position before uploading
                    
                    // Upload to Imgur
                    var uploadResult = await _imgurService.UploadImageAsync(outputStream, outputFileName);
                    
                    if (!uploadResult.Success)
                    {
                        return BadRequest(new { message = uploadResult.ErrorMessage ?? "Không thể tải ảnh bìa lên Imgur. Vui lòng thử lại." });
                    }
                    
                    coverUrl = uploadResult.DirectUrl;
                }
                else
                {
                    // Use a default cover if none provided
                    coverUrl = "/images/no-image.png";
                }
                
                // Create manga object
                var manga = new Manga
                {
                    Title = model.Title,
                    AlternativeTitle = model.AlternativeTitle,
                    Description = model.Description,
                    CoverUrl = coverUrl,
                    Author = model.Author,
                    Artist = model.Artist,
                    Status = model.Status,
                    PublicationYear = model.PublicationYear,
                    OriginalLanguage = "vi" // Default to Vietnamese for uploaded manga
                };
                
                // Get current user ID
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                
                // Save manga to database
                var createdManga = await _mangaRepository.CreateMangaAsync(manga, model.GenreIds, userId);
                
                return Ok(new { mangaId = createdManga.MangaId, message = "Tạo truyện thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating manga");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi máy chủ khi tạo truyện." });
            }
        }
        
        // PUT: /api/manga-upload/{mangaId}/info
        [HttpPut("{mangaId}/info")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateManga(int mangaId, [FromForm] MangaUpdateDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Dữ liệu không hợp lệ.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }
            
            try
            {
                // Check if manga exists and user is the uploader
                var existingManga = await _mangaRepository.GetByIdAsync(mangaId);
                if (existingManga == null)
                {
                    return NotFound(new { message = "Không tìm thấy truyện." });
                }
                
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (existingManga.UploadedByUserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }
                
                // Upload new cover image if provided
                string newCoverUrl = null;
                if (model.CoverFile != null && model.CoverFile.Length > 0)
                {
                    if (model.CoverFile.Length > MaxFileSize)
                    {
                        return BadRequest(new { message = $"Kích thước file quá lớn (tối đa {MaxFileSize / 1024 / 1024} MB)." });
                    }
                    
                    var fileExtension = Path.GetExtension(model.CoverFile.FileName).ToLowerInvariant();
                    var mimeType = model.CoverFile.ContentType.ToLowerInvariant();
                    
                    if (string.IsNullOrEmpty(fileExtension) || !AllowedExtensions.Contains(fileExtension) || !AllowedMimeTypes.Contains(mimeType))
                    {
                        return BadRequest(new { message = "Định dạng file không hợp lệ. Chỉ chấp nhận JPG, PNG, GIF." });
                    }
                    
                    // Process image with ImageSharp
                    using var inputStream = model.CoverFile.OpenReadStream();
                    using var image = await Image.LoadAsync(inputStream);
                    
                    bool resized = false;
                    if (image.Width > MaxImageWidth || image.Height > MaxImageHeight)
                    {
                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(MaxImageWidth, MaxImageHeight),
                            Mode = ResizeMode.Max // Maintain aspect ratio, fit within bounds
                        }));
                        resized = true;
                    }
                    
                    // Save the (potentially resized) image to a new stream
                    using var outputStream = new MemoryStream();
                    string outputFileName = Path.ChangeExtension(model.CoverFile.FileName, ".jpg");
                    await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = JpegQuality });
                    outputStream.Position = 0; // Reset stream position before uploading
                    
                    // Upload to Imgur
                    var uploadResult = await _imgurService.UploadImageAsync(outputStream, outputFileName);
                    
                    if (!uploadResult.Success)
                    {
                        return BadRequest(new { message = uploadResult.ErrorMessage ?? "Không thể tải ảnh bìa lên Imgur. Vui lòng thử lại." });
                    }
                    
                    newCoverUrl = uploadResult.DirectUrl;
                }
                
                // Update manga object
                var mangaToUpdate = new Manga
                {
                    Title = model.Title,
                    AlternativeTitle = model.AlternativeTitle,
                    Description = model.Description,
                    Author = model.Author,
                    Artist = model.Artist,
                    Status = model.Status,
                    PublicationYear = model.PublicationYear
                };
                
                // Update manga in database
                var updatedManga = await _mangaRepository.UpdateMangaAsync(mangaId, mangaToUpdate, model.GenreIds, newCoverUrl);
                
                return Ok(new { message = "Cập nhật truyện thành công." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Không tìm thấy truyện." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating manga {mangaId}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi máy chủ khi cập nhật truyện." });
            }
        }
        
        // GET: /api/manga-upload/user-manga
        [HttpGet("user-manga")]
        public async Task<IActionResult> GetUserMangas()
        {
            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var mangas = await _mangaRepository.GetMangasByUploaderAsync(userId);
                
                return Ok(mangas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user mangas");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi máy chủ khi lấy danh sách truyện." });
            }
        }
        
        // GET: /api/manga-upload/{mangaId}/chapters
        [HttpGet("{mangaId}/chapters")]
        public async Task<IActionResult> GetMangaChapters(int mangaId)
        {
            try
            {
                // Check if manga exists and user is the uploader
                var manga = await _mangaRepository.GetByIdAsync(mangaId);
                if (manga == null)
                {
                    return NotFound(new { message = "Không tìm thấy truyện." });
                }
                
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (manga.UploadedByUserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }
                
                var chapters = await _chapterRepository.GetChaptersByMangaIdAsync(mangaId);
                
                return Ok(chapters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting chapters for manga {mangaId}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi máy chủ khi lấy danh sách chapter." });
            }
        }
        
        // POST: /api/manga-upload/{mangaId}/chapter/create
        [HttpPost("{mangaId}/chapter/create")]
        public async Task<IActionResult> CreateChapter(int mangaId, [FromBody] ChapterUploadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            try
            {
                // Check if manga exists and user is the uploader
                var manga = await _mangaRepository.GetByIdAsync(mangaId);
                if (manga == null)
                {
                    return NotFound(new { message = "Không tìm thấy truyện." });
                }
                
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (manga.UploadedByUserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }
                
                // Create chapter object
                var chapter = new Chapter
                {
                    MangaId = mangaId,
                    Title = model.Title,
                    ChapterNumber = model.ChapterNumber,
                    LanguageCode = model.LanguageCode,
                    UploadDate = DateTime.Now,
                    SourceId = manga.SourceId // Use the same source as the manga
                };
                
                // Save chapter to database
                var createdChapter = await _chapterRepository.CreateChapterAsync(chapter);
                
                return Ok(new { chapterId = createdChapter.ChapterId, message = "Tạo chapter thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating chapter for manga {mangaId}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi máy chủ khi tạo chapter." });
            }
        }
        
        // PUT: /api/manga-upload/chapter/{chapterId}/info
        [HttpPut("chapter/{chapterId}/info")]
        public async Task<IActionResult> UpdateChapter(int chapterId, [FromBody] ChapterUploadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            try
            {
                // Check if chapter exists
                var chapter = await _chapterRepository.GetChapterByIdAsync(chapterId);
                if (chapter == null)
                {
                    return NotFound(new { message = "Không tìm thấy chapter." });
                }
                
                // Check if user is the uploader of the manga
                var manga = await _mangaRepository.GetByIdAsync(chapter.MangaId.Value);
                if (manga == null)
                {
                    return NotFound(new { message = "Không tìm thấy truyện." });
                }
                
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (manga.UploadedByUserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }
                
                // Update chapter object
                var chapterData = new Chapter
                {
                    Title = model.Title,
                    ChapterNumber = model.ChapterNumber,
                    LanguageCode = model.LanguageCode
                };
                
                // Update chapter in database
                var updatedChapter = await _chapterRepository.UpdateChapterAsync(chapterId, chapterData);
                
                return Ok(new { message = "Cập nhật chapter thành công." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Không tìm thấy chapter." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating chapter {chapterId}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi máy chủ khi cập nhật chapter." });
            }
        }
        
        // DELETE: /api/manga-upload/chapter/{chapterId}
        [HttpDelete("chapter/{chapterId}")]
        public async Task<IActionResult> DeleteChapter(int chapterId)
        {
            try
            {
                // Check if chapter exists
                var chapter = await _chapterRepository.GetChapterByIdAsync(chapterId);
                if (chapter == null)
                {
                    return NotFound(new { message = "Không tìm thấy chapter." });
                }
                
                // Check if user is the uploader of the manga
                var manga = await _mangaRepository.GetByIdAsync(chapter.MangaId.Value);
                if (manga == null)
                {
                    return NotFound(new { message = "Không tìm thấy truyện." });
                }
                
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (manga.UploadedByUserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }
                
                // Delete chapter from database
                await _chapterRepository.DeleteChapterAsync(chapterId);
                
                return Ok(new { message = "Xóa chapter thành công." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Không tìm thấy chapter." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting chapter {chapterId}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi máy chủ khi xóa chapter." });
            }
        }
        
        // DELETE: /api/manga-upload/{mangaId}
        [HttpDelete("{mangaId}")]
        public async Task<IActionResult> DeleteManga(int mangaId)
        {
            try
            {
                // Check if manga exists and user is the uploader
                var manga = await _mangaRepository.GetByIdAsync(mangaId);
                if (manga == null)
                {
                    return NotFound(new { message = "Không tìm thấy truyện." });
                }
                
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (manga.UploadedByUserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }
                
                // Delete manga from database
                await _mangaRepository.DeleteMangaAsync(mangaId);
                
                return Ok(new { message = "Xóa truyện thành công." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Không tìm thấy truyện." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting manga {mangaId}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi máy chủ khi xóa truyện." });
            }
        }
        
        // POST: /api/manga-upload/chapter/{chapterId}/upload-page
        [HttpPost("chapter/{chapterId}/upload-page")]
        public async Task<IActionResult> UploadPage(int chapterId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Không có file nào được chọn." });
            }
            
            try
            {
                // Check if chapter exists
                var chapter = await _chapterRepository.GetChapterByIdAsync(chapterId);
                if (chapter == null)
                {
                    return NotFound(new { message = "Không tìm thấy chapter." });
                }
                
                // Check if user is the uploader of the manga
                var manga = await _mangaRepository.GetByIdAsync(chapter.MangaId.Value);
                if (manga == null)
                {
                    return NotFound(new { message = "Không tìm thấy truyện." });
                }
                
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (manga.UploadedByUserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }
                
                // Validate file
                if (file.Length > MaxFileSize)
                {
                    return BadRequest(new { message = $"Kích thước file quá lớn (tối đa {MaxFileSize / 1024 / 1024} MB)." });
                }
                
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var mimeType = file.ContentType.ToLowerInvariant();
                
                if (string.IsNullOrEmpty(fileExtension) || !AllowedExtensions.Contains(fileExtension) || !AllowedMimeTypes.Contains(mimeType))
                {
                    return BadRequest(new { message = "Định dạng file không hợp lệ. Chỉ chấp nhận JPG, PNG, GIF." });
                }
                
                // Upload to Imgur
                using var stream = file.OpenReadStream();
                var uploadResult = await _imgurService.UploadImageAsync(stream, file.FileName);
                
                if (!uploadResult.Success)
                {
                    return BadRequest(new { message = uploadResult.ErrorMessage ?? "Không thể tải ảnh lên Imgur. Vui lòng thử lại." });
                }
                
                // Return the result
                var result = new PageUploadResult
                {
                    ImageUrl = uploadResult.DirectUrl,
                    OriginalFileName = file.FileName
                };
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading page for chapter {chapterId}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi máy chủ khi tải trang lên." });
            }
        }
        
        // GET: /api/manga-upload/chapter/{chapterId}/pages
        [HttpGet("chapter/{chapterId}/pages")]
        public async Task<IActionResult> GetChapterPages(int chapterId)
        {
            try
            {
                // Check if chapter exists
                var chapter = await _chapterRepository.GetChapterByIdAsync(chapterId);
                if (chapter == null)
                {
                    return NotFound(new { message = "Không tìm thấy chapter." });
                }
                
                // Check if user is the uploader of the manga
                var manga = await _mangaRepository.GetByIdAsync(chapter.MangaId.Value);
                if (manga == null)
                {
                    return NotFound(new { message = "Không tìm thấy truyện." });
                }
                
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (manga.UploadedByUserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }
                
                // Get pages from database
                var pages = await _chapterRepository.GetPagesByChapterIdAsync(chapterId);
                
                return Ok(pages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting pages for chapter {chapterId}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi máy chủ khi lấy danh sách trang." });
            }
        }
        
        // POST: /api/manga-upload/chapter/{chapterId}/save-pages
        [HttpPost("chapter/{chapterId}/save-pages")]
        public async Task<IActionResult> SavePages(int chapterId, [FromBody] SavePagesRequest request)
        {
            if (request == null || request.OrderedImageUrls == null)
            {
                return BadRequest(new { message = "Dữ liệu không hợp lệ." });
            }
            
            try
            {
                // Check if chapter exists
                var chapter = await _chapterRepository.GetChapterByIdAsync(chapterId);
                if (chapter == null)
                {
                    return NotFound(new { message = "Không tìm thấy chapter." });
                }
                
                // Check if user is the uploader of the manga
                var manga = await _mangaRepository.GetByIdAsync(chapter.MangaId.Value);
                if (manga == null)
                {
                    return NotFound(new { message = "Không tìm thấy truyện." });
                }
                
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (manga.UploadedByUserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }
                
                // Update pages in database
                await _chapterRepository.UpdateChapterPagesAsync(chapterId, request.OrderedImageUrls);
                
                return Ok(new { message = "Lưu thứ tự trang thành công." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Không tìm thấy chapter." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving pages for chapter {chapterId}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi máy chủ khi lưu thứ tự trang." });
            }
        }
    }
}
