using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mangareading.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace Mangareading.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Require authentication for all actions in this controller
    public class UploadController : ControllerBase
    {
        private readonly IImgurService _imgurService;
        private readonly ILogger<UploadController> _logger;
        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private static readonly string[] AllowedMimeTypes = { "image/jpeg", "image/png", "image/gif" };
        private const int MaxImageWidth = 800; // Max width in pixels
        private const int MaxImageHeight = 800; // Max height in pixels
        private const int JpegQuality = 85; // Quality for resized JPEGs

        public UploadController(IImgurService imgurService, ILogger<UploadController> logger)
        {
            _imgurService = imgurService;
            _logger = logger;
        }

        [HttpPost("avatar")]
        [ValidateAntiForgeryToken] // Add anti-forgery token validation
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Không có file nào được chọn." });
            }

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

            try
            {
                 _logger.LogInformation("Received avatar upload request for user {UserId}, file: {FileName}, size: {Size}, type: {ContentType}", 
                    User.FindFirstValue(ClaimTypes.NameIdentifier), file.FileName, file.Length, file.ContentType);

                 // Process image with ImageSharp
                await using var inputStream = file.OpenReadStream();
                using var image = await Image.LoadAsync(inputStream);
                
                bool resized = false;
                if (image.Width > MaxImageWidth || image.Height > MaxImageHeight)
                {
                     _logger.LogInformation("Image {FileName} ({Width}x{Height}) exceeds max dimensions ({MaxWidth}x{MaxHeight}). Resizing.", 
                        file.FileName, image.Width, image.Height, MaxImageWidth, MaxImageHeight);
                        
                     image.Mutate(x => x.Resize(new ResizeOptions
                     {
                         Size = new Size(MaxImageWidth, MaxImageHeight),
                         Mode = ResizeMode.Max // Maintain aspect ratio, fit within bounds
                     }));
                     resized = true;
                     _logger.LogInformation("Resized image {FileName} to {Width}x{Height}", file.FileName, image.Width, image.Height);
                }

                // Save the (potentially resized) image to a new stream, preferably as JPEG for size
                await using var outputStream = new MemoryStream();
                string outputFileName = Path.ChangeExtension(file.FileName, ".jpg"); // Suggest .jpg extension
                await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = JpegQuality });
                outputStream.Position = 0; // Reset stream position before uploading

                _logger.LogInformation("Prepared image {FileName} for Imgur upload (Resized: {Resized}, Output Size: {Size} bytes)", 
                    outputFileName, resized, outputStream.Length);

                // Upload the processed stream
                ImgurUploadResult uploadResult = await _imgurService.UploadImageAsync(outputStream, outputFileName);

                if (uploadResult.Success)
                {
                    _logger.LogInformation("Successfully uploaded avatar for user {UserId} to {ImageUrl}", User.FindFirstValue(ClaimTypes.NameIdentifier), uploadResult.DirectUrl);
                    return Ok(new { imageUrl = uploadResult.DirectUrl });
                }
                else
                {
                     _logger.LogError("Imgur service failed to upload avatar for user {UserId}, file: {FileName} (Resized: {Resized}). Error: {Error}, Status Code: {StatusCode}", 
                        User.FindFirstValue(ClaimTypes.NameIdentifier), outputFileName, resized, uploadResult.ErrorMessage, uploadResult.StatusCode);
                    return BadRequest(new { message = uploadResult.ErrorMessage ?? "Không thể tải ảnh lên Imgur. Vui lòng thử lại." });
                }
            }
            catch (UnknownImageFormatException ex) // Catch ImageSharp specific format errors
            {
                 _logger.LogError(ex, "Could not process uploaded file {FileName} as a valid image format.", file.FileName);
                 return BadRequest(new { message = "File tải lên không phải là định dạng ảnh hợp lệ hoặc bị lỗi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing or uploading avatar for user {UserId}, file: {FileName}", User.FindFirstValue(ClaimTypes.NameIdentifier), file.FileName);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi máy chủ khi xử lý hoặc tải ảnh lên." });
            }
        }
    }
} 