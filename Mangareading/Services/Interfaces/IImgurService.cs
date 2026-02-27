using System.IO;
using System.Threading.Tasks;

namespace Mangareading.Services.Interfaces
{
    /// <summary>
    /// Represents the result of an Imgur upload operation.
    /// </summary>
    public class ImgurUploadResult
    {
        public bool Success => string.IsNullOrEmpty(ErrorMessage);
        public string? DirectUrl { get; set; }
        public string? ErrorMessage { get; set; }
        public int? StatusCode { get; set; } // Optional: Store HTTP status code for specific errors
    }

    /// <summary>
    /// Provides functionality to interact with the Imgur API, specifically for resolving image URLs and uploading images.
    /// </summary>
    public interface IImgurService
    {
        /// <summary>
        /// Attempts to resolve various Imgur URL types (direct link, gallery, album) 
        /// to a direct image URL (.jpg, .png, etc.).
        /// </summary>
        /// <param name="imgurUrl">The Imgur URL provided by the user.</param>
        /// <returns>The direct image URL if successful, otherwise null.</returns>
        Task<string?> ResolveDirectImageUrlAsync(string imgurUrl);

        /// <summary>
        /// Uploads an image stream to Imgur anonymously.
        /// </summary>
        /// <param name="imageStream">The stream containing the image data.</param>
        /// <param name="fileName">Optional file name for the uploaded image.</param>
        /// <returns>An ImgurUploadResult indicating success or failure, including the direct URL or an error message.</returns>
        Task<ImgurUploadResult> UploadImageAsync(Stream imageStream, string? fileName = null);
    }
} 