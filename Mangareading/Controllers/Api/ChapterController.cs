using System;
using System.Threading.Tasks;
using Mangareading.Models;
using Mangareading.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mangareading.Repositories;

namespace Mangareading.Controllers.Api
{
    [Route("api/chapter")]
    [ApiController]
    public class ChapterController : ControllerBase
    {
        private readonly IChapterRepository _chapterRepository;
        private readonly ILogger<ChapterController> _logger;

        public ChapterController(
            IChapterRepository chapterRepository,
            ILogger<ChapterController> logger)
        {
            _chapterRepository = chapterRepository;
            _logger = logger;
        }

        // GET: api/chapter/{chapterId}
        [HttpGet("{chapterId}")]
        public async Task<IActionResult> GetChapter(int chapterId)
        {
            try
            {
                var chapter = await _chapterRepository.GetChapterByIdAsync(chapterId);
                if (chapter == null)
                {
                    return NotFound(new { message = "Không tìm thấy chapter." });
                }

                return Ok(chapter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting chapter {chapterId}");
                return StatusCode(500, new { message = "Lỗi máy chủ khi lấy thông tin chapter." });
            }
        }

        // GET: api/chapter/{chapterId}/pages
        [HttpGet("{chapterId}/pages")]
        public async Task<IActionResult> GetChapterPages(int chapterId)
        {
            try
            {
                var pages = await _chapterRepository.GetChapterPagesAsync(chapterId);
                if (pages == null)
                {
                    return NotFound(new { message = "Không tìm thấy trang cho chapter này." });
                }

                return Ok(pages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting pages for chapter {chapterId}");
                return StatusCode(500, new { message = "Lỗi máy chủ khi lấy danh sách trang." });
            }
        }
    }
}
