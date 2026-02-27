using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Mangareading.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Mangareading.Services
{
    public class ImgurService : IImgurService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ImgurService> _logger;
        private readonly string? _clientId;
        private readonly string? _clientSecret; // Added
        private string? _currentRefreshToken; // Added: Stores the initial or latest refresh token
        private string? _currentAccessToken; // Added: Stores the current access token
        private DateTime _accessTokenExpiry = DateTime.MinValue; // Added: Stores expiry time
        private static readonly SemaphoreSlim _tokenRefreshSemaphore = new SemaphoreSlim(1, 1); // Semaphore for token refresh

        private const string ImgurApiBaseUrl = "https://api.imgur.com/3/";
        private const string ImgurTokenUrl = "https://api.imgur.com/oauth2/token"; // Added

        // Rate limiting variables
        private static readonly SemaphoreSlim _uploadSemaphore = new SemaphoreSlim(1, 1); // Allow only 1 upload at a time
        private static DateTime _lastUploadTime = DateTime.MinValue;
        private const int MIN_UPLOAD_INTERVAL_MS = 2000; // 2 seconds between uploads to avoid rate limiting

        // Regex to extract hash from various Imgur URL formats
        private static readonly Regex ImgurUrlRegex = new Regex(
            @"imgur\.com/(?:gallery/|a/|)(?<hash>[a-zA-Z0-9]{5,})(?:\.[a-zA-Z]+)?(?:#|\?|$)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        // Regex to check if it's already a direct image link
         private static readonly Regex DirectImageRegex = new Regex(
            @"^https?://i\.imgur\.com/[a-zA-Z0-9]+\.(?:jpg|jpeg|png|gif|gifv|mp4|webp)(\?.*)?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public ImgurService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ImgurService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Read credentials from configuration
            _clientId = _configuration["ExternalServices:Imgur:ClientId"];
            _clientSecret = _configuration["ExternalServices:Imgur:ClientSecret"]; // Read secret
            _currentRefreshToken = _configuration["ExternalServices:Imgur:RefreshToken"]; // Read initial refresh token

            // Validate configuration
            bool hasClientId = !string.IsNullOrWhiteSpace(_clientId) && _clientId != "YOUR_IMGUR_CLIENT_ID";
            bool hasClientSecret = !string.IsNullOrWhiteSpace(_clientSecret) && _clientSecret != "YOUR_IMGUR_CLIENT_SECRET";
            bool hasRefreshToken = !string.IsNullOrWhiteSpace(_currentRefreshToken) && _currentRefreshToken != "YOUR_INITIAL_IMGUR_REFRESH_TOKEN";

            if (hasRefreshToken && hasClientId && hasClientSecret)
            {
                _logger.LogInformation("Imgur configured for authenticated access using Refresh Token.");
                // Optionally trigger an initial token refresh here or do it lazily before the first API call
            }
            else if (hasClientId)
            {
                _logger.LogInformation("Imgur configured for anonymous access using Client ID only. Refresh token/secret missing or invalid.");
                _currentRefreshToken = null; // Ensure refresh isn't attempted
            }
            else
            {
                _logger.LogError("Imgur configuration is incomplete. Both Client ID and Refresh Token/Secret are missing or invalid. Imgur service may not function.");
                _clientId = null; // Ensure ClientId is null if unusable
                _currentRefreshToken = null;
            }
        }

        // --- Token Refresh Logic ---

        private async Task<bool> RefreshAccessTokenAsync()
        {
            if (string.IsNullOrWhiteSpace(_currentRefreshToken) || string.IsNullOrWhiteSpace(_clientId) || string.IsNullOrWhiteSpace(_clientSecret))
            {
                _logger.LogWarning("Cannot refresh Imgur token: Missing Refresh Token, Client ID, or Client Secret.");
                return false;
            }

            // Use semaphore to prevent concurrent refresh attempts
            await _tokenRefreshSemaphore.WaitAsync();
            try
            {
                // Double-check if token was refreshed by another thread while waiting
                if (_accessTokenExpiry > DateTime.UtcNow)
                {
                    return true; // Already refreshed
                }

                _logger.LogInformation("Attempting to refresh Imgur access token using refresh token.");
                var client = _httpClientFactory.CreateClient("ImgurTokenClient");

                var requestData = new Dictionary<string, string>
                {
                    { "refresh_token", _currentRefreshToken },
                    { "client_id", _clientId },
                    { "client_secret", _clientSecret },
                    { "grant_type", "refresh_token" }
                };

                using var content = new FormUrlEncodedContent(requestData);
                HttpResponseMessage response = await client.PostAsync(ImgurTokenUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("Imgur token refresh successful. Response: {Response}", jsonResponse);
                    var json = JObject.Parse(jsonResponse);

                    _currentAccessToken = json["access_token"]?.Value<string>();
                    string? newRefreshToken = json["refresh_token"]?.Value<string>();
                    int expiresIn = json["expires_in"]?.Value<int>() ?? 3600; // Default to 1 hour

                    if (!string.IsNullOrWhiteSpace(newRefreshToken))
                    {
                        _currentRefreshToken = newRefreshToken; // Update refresh token if Imgur provides a new one
                        _logger.LogInformation("Received a new Imgur refresh token.");
                        // Consider logging or storing this new refresh token securely if needed for long-term use
                    }

                    if (!string.IsNullOrWhiteSpace(_currentAccessToken))
                    {
                        _accessTokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60); // Subtract 60s buffer
                        _logger.LogInformation("Imgur access token refreshed successfully. Expires at: {ExpiryTime}", _accessTokenExpiry);
                        return true;
                    }
                    else
                    {
                        _logger.LogError("Failed to parse access token from Imgur refresh response.");
                        return false;
                    }
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to refresh Imgur access token. Status: {StatusCode}, Response: {ErrorResponse}", response.StatusCode, errorContent);
                    // If refresh token becomes invalid, clear it to prevent further attempts?
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                         _logger.LogError("Imgur Refresh Token might be invalid or expired. Clearing current refresh token.");
                         _currentRefreshToken = null; // Stop trying to refresh
                    }
                    _currentAccessToken = null; // Ensure access token is cleared
                    _accessTokenExpiry = DateTime.MinValue;
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while refreshing the Imgur access token.");
                _currentAccessToken = null;
                _accessTokenExpiry = DateTime.MinValue;
                return false;
            }
            finally
            {
                _tokenRefreshSemaphore.Release();
            }
        }

        private async Task<string?> GetValidAccessTokenAsync()
        {
            if (_accessTokenExpiry > DateTime.UtcNow)
            {
                return _currentAccessToken; // Current token is valid
            }

            // If token expired or never fetched, try refreshing
            if (await RefreshAccessTokenAsync())
            {
                return _currentAccessToken; // Return newly refreshed token
            }

            return null; // Refresh failed
        }

        // --- Modified API Call Logic ---

        public async Task<string?> ResolveDirectImageUrlAsync(string imgurUrl)
        {
            if (string.IsNullOrWhiteSpace(imgurUrl))
            {
                return null;
            }

            // 1. Check if it's already a direct i.imgur.com link
            if (DirectImageRegex.IsMatch(imgurUrl))
            {
                 _logger.LogInformation("Provided URL is already a direct Imgur link: {Url}", imgurUrl);
                // Optional: Strip query parameters if needed, but generally direct links work with them.
                return imgurUrl.Split('?')[0];
            }

            // Check if authentication is possible
            string? accessToken = await GetValidAccessTokenAsync();
            bool canAuthenticate = accessToken != null || (!string.IsNullOrWhiteSpace(_clientId) && _clientId != "YOUR_IMGUR_CLIENT_ID");

            if (!canAuthenticate)
            {
                 _logger.LogError("Cannot resolve Imgur URL: No valid authentication method (Access Token or Client ID).");
                 return null;
            }

            // 3. Extract hash from other Imgur URL types
            var match = ImgurUrlRegex.Match(imgurUrl);
            if (!match.Success)
            {
                _logger.LogWarning("Could not extract Imgur hash from URL: {Url}", imgurUrl);
                return null;
            }

            string hash = match.Groups["hash"].Value;
            _logger.LogInformation("Extracted Imgur hash '{Hash}' from URL: {Url}", hash, imgurUrl);

            // 4. Try fetching as an image first (most common case after direct link)
            string? directUrl = await GetImageDataAsync(hash);

            // 5. If not found as image, try fetching as an album
            if (directUrl == null)
            {
                _logger.LogInformation("Hash '{Hash}' not found as image, trying as album.", hash);
                directUrl = await GetAlbumDataAsync(hash);
            }

            if (directUrl != null)
            {
                 _logger.LogInformation("Successfully resolved Imgur URL {Url} to {DirectUrl}", imgurUrl, directUrl);
            }
            else
            {
                 _logger.LogWarning("Failed to resolve Imgur hash '{Hash}' to a direct image URL from URL: {Url}", hash, imgurUrl);
            }

            return directUrl;
        }

        private async Task<string?> GetImageDataAsync(string hash)
        {
            return await CallImgurApiAsync<string>($"image/{hash}", ParseImageResponse);
        }

        private async Task<string?> GetAlbumDataAsync(string hash)
        {
             return await CallImgurApiAsync<string>($"album/{hash}/images", ParseAlbumResponse);
        }

        private async Task<T?> CallImgurApiAsync<T>(string endpoint, Func<string, T?> parseResponse) where T : class // Add class constraint if T is always a reference type
        {
            string? accessToken = await GetValidAccessTokenAsync();
            bool useBearerAuth = accessToken != null;
            bool useClientIdAuth = !useBearerAuth && !string.IsNullOrWhiteSpace(_clientId) && _clientId != "YOUR_IMGUR_CLIENT_ID";

            if (!useBearerAuth && !useClientIdAuth)
            {
                _logger.LogError("Imgur API call skipped for {Endpoint}: No valid authentication method.", endpoint);
                return default;
            }

            var client = _httpClientFactory.CreateClient("ImgurApiClient"); // Consider defining a named client
            client.BaseAddress = new Uri(ImgurApiBaseUrl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Set Authorization header
            if (useBearerAuth)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                _logger.LogDebug("Using Bearer token for Imgur API call to {Endpoint}.");
            }
            else // Fallback to Client-ID
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Client-ID", _clientId);
                _logger.LogDebug("Using Client-ID for anonymous Imgur API call to {Endpoint}.");
            }

            try
            {
                _logger.LogDebug("Calling Imgur API endpoint: {Endpoint}", endpoint);
                HttpResponseMessage response = await client.GetAsync(endpoint);
                // Log rate limits regardless of success/failure for debugging
                LogRateLimitHeaders(response.Headers);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                     _logger.LogDebug("Imgur API success response for {Endpoint}: {Response}", endpoint, jsonResponse.Substring(0, Math.Min(jsonResponse.Length, 200)) + "...");
                    return parseResponse(jsonResponse);
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync() ?? "<empty response>"; // Handle null
                    _logger.LogError("Imgur API call failed for endpoint {Endpoint}. Status: {StatusCode}. Response: {ErrorResponse}",
                        endpoint, response.StatusCode, errorContent);
                    return default;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed when calling Imgur API endpoint {Endpoint}.", endpoint);
                return default;
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "An unexpected error occurred when calling Imgur API endpoint {Endpoint}.", endpoint);
                return default;
            }
        }

        private string? ParseImageResponse(string jsonResponse)
        {
            try
            {
                var json = JObject.Parse(jsonResponse);
                if (json["success"]?.Value<bool>() == true && json["data"]?["link"] != null)
                {
                    // Ensure it's not an animation/video if we strictly want static images
                    bool isAnimated = json["data"]?["animated"]?.Value<bool>() ?? false;
                    string? type = json["data"]?["type"]?.Value<string>();
                    string? link = json["data"]?["link"]?.Value<string>(); // Use nullable type
                    if (link == null) {
                         _logger.LogWarning("Parsed Imgur link is null.");
                         return null;
                    }
                    // Basic check: Allow common static types, exclude webm/mp4 unless desired
                     if (isAnimated && (type?.Contains("mp4") == true || type?.Contains("webm") == true))
                    {
                         _logger.LogWarning("Resolved Imgur resource is a video/animation, not a static image: {Link}", link);

                    }

                    return link;
                }
                 _logger.LogWarning("Could not parse direct link from Imgur image response: {JsonResponse}", jsonResponse.Substring(0, Math.Min(jsonResponse.Length, 200)) + "...");
                return null;
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error parsing Imgur image JSON response.");
                return null;
            }
        }

        private string? ParseAlbumResponse(string jsonResponse)
        {
            try
            {
                var json = JObject.Parse(jsonResponse);
                if (json["success"]?.Value<bool>() == true && json["data"] is JArray images && images.Count > 0)
                {
                    // Get the link of the first image in the album
                    var firstImage = images.FirstOrDefault();
                    string? link = firstImage?["link"]?.Value<string>();
                     if (!string.IsNullOrEmpty(link))
                    {
                        // Optional: Add checks similar to ParseImageResponse if needed (e.g., skip videos)
                         return link;
                    }
                }
                _logger.LogWarning("Could not parse direct link from Imgur album response or album is empty: {JsonResponse}", jsonResponse.Substring(0, Math.Min(jsonResponse.Length, 200)) + "...");
                return null;
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error parsing Imgur album JSON response.");
                return null;
            }
        }

        // Ensure return type matches IImgurService. Assuming it expects Task<ImgurUploadResult>
        public async Task<ImgurUploadResult> UploadImageAsync(Stream imageStream, string? fileName = null)
        {
            string? accessToken = await GetValidAccessTokenAsync();
            bool useBearerAuth = accessToken != null;
            bool useClientIdAuth = !useBearerAuth && !string.IsNullOrWhiteSpace(_clientId) && _clientId != "YOUR_IMGUR_CLIENT_ID";

            if (!useBearerAuth && !useClientIdAuth)
            {
                _logger.LogError("Cannot upload to Imgur: No valid authentication method.");
                return new ImgurUploadResult { ErrorMessage = "Imgur service not configured." };
            }

            if (imageStream == null || imageStream.Length == 0)
            {
                 _logger.LogError("Cannot upload to Imgur because the image stream is null or empty.");
                 return new ImgurUploadResult { ErrorMessage = "Image stream is invalid." };
            }

            // Prepare the image data before acquiring the semaphore
            using var memoryStream = new System.IO.MemoryStream();
            await imageStream.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();

            // Acquire semaphore to control upload rate
            await _uploadSemaphore.WaitAsync();

            try
            {
                // Implement rate limiting
                var timeSinceLastUpload = DateTime.Now - _lastUploadTime;
                if (timeSinceLastUpload.TotalMilliseconds < MIN_UPLOAD_INTERVAL_MS)
                {
                    // Wait to respect rate limit
                    int delayMs = MIN_UPLOAD_INTERVAL_MS - (int)timeSinceLastUpload.TotalMilliseconds;
                    _logger.LogInformation("Rate limiting: Waiting {DelayMs}ms before uploading {FileName}", delayMs, fileName ?? "N/A");
                    await Task.Delay(delayMs);
                }

                var client = _httpClientFactory.CreateClient("ImgurApiClient");
                client.BaseAddress = new Uri(ImgurApiBaseUrl);

                // Set Authorization header
                if (useBearerAuth)
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                     _logger.LogDebug("Using Bearer token for Imgur upload.");
                }
                else // Fallback to Client-ID
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Client-ID", _clientId);
                     _logger.LogDebug("Using Client-ID for anonymous Imgur upload.");
                }

                using var content = new MultipartFormDataContent();
                var imageContent = new ByteArrayContent(imageBytes);
                imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg"); // Let Imgur determine type if possible, provide a default

                // Add the image content with the name "image"
                content.Add(imageContent, "image", fileName ?? "uploaded_image.jpg");

                // Implement retry logic
                const int maxRetries = 3;
                int retryCount = 0;
                bool success = false;
                HttpResponseMessage? response = null; // Make nullable
                string? errorContent = null; // Make nullable

                while (retryCount < maxRetries && !success)
                {
                    try
                    {
                        _logger.LogInformation("Attempting to upload image {FileName} ({Size} bytes) to Imgur. Attempt {Attempt}/{MaxRetries}",
                            fileName ?? "N/A", imageBytes.Length, retryCount + 1, maxRetries);

                        response = await client.PostAsync("image", content); // Use the configured client
                        // Log rate limits regardless of success/failure for debugging
                        LogRateLimitHeaders(response.Headers);
                        success = response.IsSuccessStatusCode;

                        if (!success)
                        {
                            errorContent = await response.Content.ReadAsStringAsync() ?? "<empty response>"; // Handle null

                            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                            {
                                retryCount++;
                                if (retryCount < maxRetries)
                                {
                                    int backoffDelay = MIN_UPLOAD_INTERVAL_MS * (int)Math.Pow(2, retryCount); // Exponential backoff
                                    _logger.LogWarning("Rate limit hit for {FileName}. Retrying in {Delay}ms. Attempt {Attempt}/{MaxRetries}",
                                        fileName ?? "N/A", backoffDelay, retryCount + 1, maxRetries);
                                    await Task.Delay(backoffDelay);
                                }
                            }
                            else
                            {
                                // For other errors, don't retry
                                break;
                            }
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogError(ex, "HTTP request failed when uploading image {FileName} to Imgur. Attempt {Attempt}/{MaxRetries}",
                            fileName ?? "N/A", retryCount + 1, maxRetries);
                        retryCount++;
                        if (retryCount < maxRetries)
                        {
                            await Task.Delay(MIN_UPLOAD_INTERVAL_MS * (int)Math.Pow(2, retryCount));
                        }
                    }
                }

                // Update last upload time
                _lastUploadTime = DateTime.Now;

                if (success && response != null)
                {
                    // LogRateLimitHeaders(response.Headers); // Already logged above
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("Imgur upload success response: {Response}", jsonResponse.Substring(0, Math.Min(jsonResponse.Length, 200)) + "...");
                    // Use the same parser as GetImageDataAsync, assuming the structure is similar
                    string? directUrl = ParseImageResponse(jsonResponse);
                    if (directUrl != null)
                    {
                        _logger.LogInformation("Successfully uploaded image {FileName} to Imgur: {DirectUrl}", fileName ?? "N/A", directUrl);
                        // Return success result
                        return new ImgurUploadResult { DirectUrl = directUrl };
                    }
                    else
                    {
                        _logger.LogError("Failed to parse direct link from successful Imgur upload response for {FileName}. Response: {JsonResponse}",
                            fileName ?? "N/A", jsonResponse.Substring(0, Math.Min(jsonResponse.Length, 500)) + "...");
                        // Return failure result even though upload technically succeeded
                        return new ImgurUploadResult { ErrorMessage = "Could not parse Imgur response after upload." };
                    }
                }
                else
                {
                    _logger.LogError("Imgur API upload failed for {FileName} after {MaxRetries} attempts. Status: {StatusCode}. Response: {ErrorResponse}",
                        fileName ?? "N/A", maxRetries, response?.StatusCode ?? System.Net.HttpStatusCode.InternalServerError, errorContent ?? "<no response>"); // Handle null response
                    // Return specific error message based on status code
                    string userMessage = "Upload to Imgur failed.";
                    if (response?.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        userMessage = "Too many requests sent to Imgur. Please wait a while and try again.";
                    }
                    // Add more specific checks if needed (e.g., BadRequest for invalid image)
                    return new ImgurUploadResult { ErrorMessage = userMessage, StatusCode = response != null ? (int)response.StatusCode : 500 }; // Handle null response
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred when uploading image {FileName} to Imgur.", fileName ?? "N/A");
                return new ImgurUploadResult { ErrorMessage = "An unexpected error occurred during upload." };
            }
            finally
            {
                // Always release the semaphore
                _uploadSemaphore.Release();
            }
        }

        private void LogRateLimitHeaders(HttpResponseHeaders headers)
        {
            try
            {
                // Log Client Limits (Always present for both auth methods)
                string? clientLimit = headers.TryGetValues("X-RateLimit-ClientLimit", out var clValues) ? clValues.FirstOrDefault() : "N/A";
                string? clientRemaining = headers.TryGetValues("X-RateLimit-ClientRemaining", out var crValues) ? crValues.FirstOrDefault() : "N/A";
                _logger.LogInformation("Imgur API Rate Limit Status - Client Remaining: {ClientRemaining}/{ClientLimit}", clientRemaining, clientLimit);

                // Log User Limits (Only log if we are likely using a Bearer token)
                if (!string.IsNullOrWhiteSpace(_currentAccessToken)) // Check if we have an access token
                {
                     string? userLimit = headers.TryGetValues("X-RateLimit-UserLimit", out var ulValues) ? ulValues.FirstOrDefault() : "N/A";
                     string? userRemaining = headers.TryGetValues("X-RateLimit-UserRemaining", out var urValues) ? urValues.FirstOrDefault() : "N/A";
                     string? userReset = headers.TryGetValues("X-RateLimit-UserReset", out var urstValues) ? urstValues.FirstOrDefault() : "N/A"; // Unix timestamp
                     string userResetTime = "N/A";
                     if (long.TryParse(userReset, out long resetTimestamp))
                     {
                         userResetTime = DateTimeOffset.FromUnixTimeSeconds(resetTimestamp).ToString("o"); // ISO 8601 format
                     }
                     _logger.LogInformation("Imgur API Rate Limit Status - User Remaining: {UserRemaining}/{UserLimit} (Resets at: {UserResetTime})", userRemaining, userLimit, userResetTime);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read or log Imgur rate limit headers.");
            }
        }
    }
}