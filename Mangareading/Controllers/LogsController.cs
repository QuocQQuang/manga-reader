using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mangareading.Models;
using Mangareading.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.IO.Compression;

namespace Mangareading.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Chỉ admin mới có quyền xem log
    public class LogsController : ControllerBase
    {
        private readonly ILogService _logService;
        private readonly ILogger<LogsController> _logger;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly IConfiguration _configuration;

        public LogsController(
            ILogService logService, 
            ILogger<LogsController> logger,
            IHostEnvironment hostEnvironment,
            IConfiguration configuration)
        {
            _logService = logService;
            _logger = logger;
            _hostEnvironment = hostEnvironment;
            _configuration = configuration;
        }

        // GET: api/logs/recent
        [HttpGet("recent")]
        public async Task<ActionResult<IEnumerable<SystemLog>>> GetRecentLogs([FromQuery] int count = 50)
        {
            try
            {
                var logs = await _logService.GetRecentLogsAsync(count);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent logs");
                return StatusCode(500, "Đã xảy ra lỗi khi lấy nhật ký hệ thống.");
            }
        }

        // GET: api/logs/level/{level}
        [HttpGet("level/{level}")]
        public async Task<ActionResult<IEnumerable<SystemLog>>> GetLogsByLevel(string level, [FromQuery] int count = 50)
        {
            try
            {
                if (string.IsNullOrEmpty(level))
                {
                    return BadRequest("Cần phải cung cấp level của log.");
                }

                var logs = await _logService.GetLogsByLevelAsync(level, count);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving logs with level {level}");
                return StatusCode(500, "Đã xảy ra lỗi khi lấy nhật ký hệ thống.");
            }
        }

        // GET: api/logs/date-range
        [HttpGet("date-range")]
        public async Task<ActionResult<IEnumerable<SystemLog>>> GetLogsByDateRange(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int count = 100)
        {
            try
            {
                if (!startDate.HasValue)
                {
                    startDate = DateTime.Today.AddDays(-7);
                }

                if (!endDate.HasValue)
                {
                    endDate = DateTime.Now;
                }

                var logs = await _logService.GetLogsByDateRangeAsync(startDate.Value, endDate.Value, count);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving logs from {startDate} to {endDate}");
                return StatusCode(500, "Đã xảy ra lỗi khi lấy nhật ký hệ thống.");
            }
        }

        // POST: api/logs/clear
        [HttpPost("clear")]
        public async Task<ActionResult> ClearLogs()
        {
            try
            {
                // Xóa tất cả log files trong thư mục logs
                var logDirectory = GetLogDirectory();
                
                if (Directory.Exists(logDirectory))
                {
                    // Lưu trữ log trước khi xóa
                    await ArchiveCurrentLogs(logDirectory);
                    
                    // Xóa các file log hiện tại
                    var logFiles = Directory.GetFiles(logDirectory, "*.log");
                    foreach (var file in logFiles)
                    {
                        try
                        {
                            // Thay vì xóa file, chỉ làm sạch nội dung của nó
                            // Để tránh vấn đề về quyền truy cập file đang được sử dụng
                            System.IO.File.WriteAllText(file, string.Empty);
                            _logger.LogInformation($"Cleared log file: {file}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Could not clear log file {file}");
                        }
                    }
                    
                    // Ghi log mới để xác nhận hành động
                    _logger.LogWarning("All log files have been cleared by admin");
                    
                    return Ok(new { message = "Đã xóa tất cả nhật ký hệ thống." });
                }
                
                return NotFound("Không tìm thấy thư mục log.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing log files");
                return StatusCode(500, "Đã xảy ra lỗi khi xóa nhật ký hệ thống.");
            }
        }

        // POST: api/logs/settings
        [HttpPost("settings")]
        public ActionResult UpdateLogSettings([FromForm] string logLevel, [FromForm] bool logToFile)
        {
            try
            {
                // Xác thực input
                if (string.IsNullOrEmpty(logLevel))
                {
                    return BadRequest("Mức độ log không được để trống.");
                }

                // Xác thực mức độ log
                var validLevels = new[] { "Information", "Warning", "Error", "Debug", "Trace", "Critical" };
                if (!validLevels.Contains(logLevel))
                {
                    return BadRequest($"Mức độ log không hợp lệ. Các giá trị có thể: {string.Join(", ", validLevels)}");
                }

                // Cập nhật cài đặt trong appsettings.json
                UpdateAppSettings(logLevel, logToFile);

                // Log thông báo về việc thay đổi cài đặt
                _logger.LogInformation($"Log settings updated - Level: {logLevel}, LogToFile: {logToFile}");

                return Ok(new { message = "Đã lưu cài đặt nhật ký hệ thống." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving log settings");
                return StatusCode(500, "Đã xảy ra lỗi khi lưu cài đặt nhật ký hệ thống.");
            }
        }
        
        // Phương thức hỗ trợ lấy đường dẫn đến thư mục log
        private string GetLogDirectory()
        {
            // Thư mục logs thường nằm ở thư mục gốc hoặc trong một thư mục cụ thể
            var contentRoot = _hostEnvironment.ContentRootPath;
            var possibleLogDirectories = new[]
            {
                Path.Combine(contentRoot, "logs"),
                Path.Combine(contentRoot, "Log"),
                Path.Combine(contentRoot, "Logs")
            };
            
            foreach (var dir in possibleLogDirectories)
            {
                if (Directory.Exists(dir) && Directory.GetFiles(dir, "*.log").Any())
                {
                    return dir;
                }
            }
            
            // Nếu không tìm thấy, tạo thư mục mới
            var logDir = Path.Combine(contentRoot, "logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            
            return logDir;
        }
        
        // Phương thức lưu trữ log hiện tại vào file archive trước khi xóa
        private async Task ArchiveCurrentLogs(string logDirectory)
        {
            try
            {
                var logFiles = Directory.GetFiles(logDirectory, "*.log");
                if (!logFiles.Any())
                {
                    return;
                }
                
                var archiveDirectory = Path.Combine(logDirectory, "archives");
                if (!Directory.Exists(archiveDirectory))
                {
                    Directory.CreateDirectory(archiveDirectory);
                }
                
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var archiveFileName = Path.Combine(archiveDirectory, $"logs_archive_{timestamp}.zip");
                
                // Sử dụng ZipFile để nén các file log
                using (var memoryStream = new MemoryStream())
                {
                    using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
                    {
                        foreach (var logFile in logFiles)
                        {
                            var logContent = await System.IO.File.ReadAllTextAsync(logFile);
                            var fileName = Path.GetFileName(logFile);
                            var entry = archive.CreateEntry(fileName);
                            
                            using (var entryStream = entry.Open())
                            using (var streamWriter = new StreamWriter(entryStream))
                            {
                                await streamWriter.WriteAsync(logContent);
                            }
                        }
                    }
                    
                    // Lưu file zip
                    await System.IO.File.WriteAllBytesAsync(archiveFileName, memoryStream.ToArray());
                }
                
                _logger.LogInformation($"Archived logs to {archiveFileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving logs");
                // Không throw exception để tiếp tục quá trình xóa log
            }
        }
        
        // Cập nhật cài đặt trong appsettings.json
        private void UpdateAppSettings(string logLevel, bool logToFile)
        {
            try
            {
                // Lưu ý: Thay đổi cài đặt trong appsettings.json cần khởi động lại ứng dụng để có hiệu lực
                var configFilePath = Path.Combine(_hostEnvironment.ContentRootPath, "appsettings.json");
                
                // Đọc nội dung file
                var json = System.IO.File.ReadAllText(configFilePath);
                
                // Parse thành JSON object
                using JsonDocument document = JsonDocument.Parse(json);
                var root = document.RootElement;
                
                // Tạo một JsonObject mới để xây dựng JSON
                var newJsonObject = new Dictionary<string, object>();
                
                // Copy các phần tử từ document gốc
                foreach (var property in root.EnumerateObject())
                {
                    if (property.Name == "Logging")
                    {
                        // Cập nhật phần Logging
                        var loggingSettings = new Dictionary<string, object>();
                        
                        // Thêm LogLevel
                        var logLevelSettings = new Dictionary<string, string>
                        {
                            ["Default"] = logLevel,
                            // Đảm bảo không ghi log SQL queries
                            ["Microsoft.EntityFrameworkCore"] = "Warning",
                            ["Microsoft.EntityFrameworkCore.Database.Command"] = "None",
                            ["System.Data.SqlClient"] = "Warning",
                            ["Microsoft.Data"] = "Warning"
                        };
                        
                        loggingSettings["LogLevel"] = logLevelSettings;
                        
                        // Thêm File settings
                        loggingSettings["File"] = new Dictionary<string, bool>
                        {
                            ["Enabled"] = logToFile
                        };
                        
                        newJsonObject["Logging"] = loggingSettings;
                    }
                    else
                    {
                        // Copy các phần tử khác từ JSON gốc
                        // Chuyển đổi JsonElement thành object phù hợp
                        newJsonObject[property.Name] = ConvertJsonElementToObject(property.Value);
                    }
                }
                
                // Chuyển đổi Dictionary thành JSON string với định dạng đẹp
                var options = new JsonSerializerOptions { WriteIndented = true };
                string updatedJson = JsonSerializer.Serialize(newJsonObject, options);
                
                // Tạo một backup của file cấu hình hiện tại
                var backupPath = configFilePath + ".bak";
                System.IO.File.Copy(configFilePath, backupPath, true);
                
                // Ghi đè file cấu hình với nội dung mới
                System.IO.File.WriteAllText(configFilePath, updatedJson);
                
                _logger.LogInformation($"Updated appsettings.json with new log level: {logLevel} and logToFile: {logToFile}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating appsettings.json");
                throw;
            }
        }
        
        // Phương thức hỗ trợ để chuyển đổi JsonElement thành đối tượng C#
        private object ConvertJsonElementToObject(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var obj = new Dictionary<string, object>();
                    foreach (var property in element.EnumerateObject())
                    {
                        obj[property.Name] = ConvertJsonElementToObject(property.Value);
                    }
                    return obj;
                    
                case JsonValueKind.Array:
                    var list = new List<object>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(ConvertJsonElementToObject(item));
                    }
                    return list;
                    
                case JsonValueKind.String:
                    return element.GetString();
                    
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out int intValue))
                        return intValue;
                    if (element.TryGetInt64(out long longValue))
                        return longValue;
                    return element.GetDouble();
                    
                case JsonValueKind.True:
                    return true;
                    
                case JsonValueKind.False:
                    return false;
                    
                case JsonValueKind.Null:
                    return null;
                    
                default:
                    return null;
            }
        }
    }
}