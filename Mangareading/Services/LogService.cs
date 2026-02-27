using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mangareading.Models;
using Mangareading.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mangareading.Services
{
    public class LogService : ILogService
    {
        private readonly ILogger<LogService> _logger;
        private readonly IHostEnvironment _hostEnvironment;
        
        // Regex patterns cho các định dạng log phổ biến
        private readonly Regex _aspNetCoreLogRegex = new Regex(@"(\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d+)\s+(\w+)\s+\[(.+?)\]\s+(.+)");
        private readonly Regex _simpleLogRegex = new Regex(@"(\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2})\s+-\s+(\w+)\s+-\s+(.+)");
        private readonly Regex _exceptionRegex = new Regex(@"Exception:\s+(.+)");
        private readonly Regex _stackTraceRegex = new Regex(@"^\s+at\s+.+$", RegexOptions.Multiline);
        
        public LogService(ILogger<LogService> logger, IHostEnvironment hostEnvironment)
        {
            _logger = logger;
            _hostEnvironment = hostEnvironment;
        }
        
        public async Task<List<SystemLog>> GetRecentLogsAsync(int count = 50)
        {
            try
            {
                var logs = await ReadLogsFromFilesAsync();
                return logs.OrderByDescending(l => l.Timestamp)
                    .Take(count)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đọc log gần đây");
                return new List<SystemLog>();
            }
        }
        
        public async Task<List<SystemLog>> GetLogsByLevelAsync(string level, int count = 50)
        {
            try
            {
                var logs = await ReadLogsFromFilesAsync();
                return logs.Where(l => l.Level.Equals(level, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(l => l.Timestamp)
                    .Take(count)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi đọc log với level {level}");
                return new List<SystemLog>();
            }
        }
        
        public async Task<List<SystemLog>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate, int count = 100)
        {
            try
            {
                var logs = await ReadLogsFromFilesAsync();
                return logs.Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                    .OrderByDescending(l => l.Timestamp)
                    .Take(count)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi đọc log trong khoảng thời gian từ {startDate} đến {endDate}");
                return new List<SystemLog>();
            }
        }
        
        private async Task<List<SystemLog>> ReadLogsFromFilesAsync()
        {
            var logs = new List<SystemLog>();
            
            try
            {
                // Tìm thư mục logs
                var logDirectory = GetLogDirectory();
                if (!Directory.Exists(logDirectory))
                {
                    _logger.LogWarning($"Thư mục log không tồn tại: {logDirectory}");
                    // Tạo thư mục logs nếu chưa tồn tại
                    Directory.CreateDirectory(logDirectory);
                    
                    // Ghi một log mẫu để có thể đọc
                    WriteInitialLogFile(logDirectory);
                }
                
                // Đọc tất cả các file log (lấy 5 file mới nhất)
                var logFiles = Directory.GetFiles(logDirectory, "*.log")
                    .OrderByDescending(f => new FileInfo(f).CreationTime)
                    .Take(5)
                    .ToList();
                
                if (!logFiles.Any())
                {
                    _logger.LogWarning($"Không tìm thấy file log nào trong thư mục: {logDirectory}");
                    WriteInitialLogFile(logDirectory);
                    logFiles = Directory.GetFiles(logDirectory, "*.log").ToList();
                }
                
                foreach (var logFile in logFiles)
                {
                    try
                    {
                        string[] fileContent;
                        
                        // Kiểm tra xem file có đang được sử dụng không
                        using (var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var sr = new StreamReader(fs))
                        {
                            var content = await sr.ReadToEndAsync();
                            fileContent = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                        }
                        
                        var parsedLogs = ParseLogContent(fileContent, Path.GetFileName(logFile));
                        logs.AddRange(parsedLogs);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Không thể đọc file log: {logFile}");
                    }
                }
                
                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đọc các file log");
                return logs;
            }
        }
        
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
            
            // Nếu không tìm thấy, trả về thư mục mặc định
            return Path.Combine(contentRoot, "logs");
        }
        
        private void WriteInitialLogFile(string logDirectory)
        {
            try
            {
                var logFilePath = Path.Combine(logDirectory, $"app_{DateTime.Now:yyyyMMdd}.log");
                
                // Tạo một số log mẫu để hiển thị
                var logEntries = new List<string>
                {
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} INFO [LogService] Hệ thống log được khởi tạo",
                    $"{DateTime.Now.AddSeconds(-5):yyyy-MM-dd HH:mm:ss.fff} WARNING [LogService] Không tìm thấy file log nào trước đó",
                    $"{DateTime.Now.AddSeconds(-10):yyyy-MM-dd HH:mm:ss.fff} INFO [System] Ứng dụng đã khởi động thành công",
                    $"{DateTime.Now.AddMinutes(-1):yyyy-MM-dd HH:mm:ss.fff} ERROR [DatabaseService] Lỗi kết nối cơ sở dữ liệu",
                    "Exception: System.Data.SqlClient.SqlException: Connection timeout",
                    "   at Database.Connect() in DatabaseService.cs:line 42",
                    "   at System.Threading.Tasks.Task.Execute()"
                };
                
                // Ghi log mẫu vào file
                File.WriteAllLines(logFilePath, logEntries);
                
                _logger.LogInformation($"Đã tạo file log mẫu: {logFilePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo file log mẫu");
            }
        }
        
        private List<SystemLog> ParseLogContent(string[] lines, string fileName)
        {
            var logs = new List<SystemLog>();
            
            SystemLog currentLog = null;
            var exceptionDetails = new List<string>();
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                
                var aspNetMatch = _aspNetCoreLogRegex.Match(line);
                var simpleMatch = _simpleLogRegex.Match(line);
                var exceptionMatch = _exceptionRegex.Match(line);
                var stackTraceMatch = _stackTraceRegex.IsMatch(line);
                
                if (aspNetMatch.Success)
                {
                    // Lưu log trước đó (nếu có) vào danh sách
                    if (currentLog != null)
                    {
                        if (exceptionDetails.Count > 0)
                        {
                            currentLog.Exception = string.Join(Environment.NewLine, exceptionDetails);
                        }
                        logs.Add(currentLog);
                        exceptionDetails.Clear();
                    }
                    
                    // Tạo log mới
                    currentLog = new SystemLog
                    {
                        Id = Guid.NewGuid().ToString(),
                        Timestamp = ParseDateTime(aspNetMatch.Groups[1].Value),
                        Level = NormalizeLogLevel(aspNetMatch.Groups[2].Value),
                        Source = aspNetMatch.Groups[3].Value,
                        Message = aspNetMatch.Groups[4].Value,
                        Details = line
                    };
                }
                else if (simpleMatch.Success)
                {
                    // Lưu log trước đó (nếu có) vào danh sách
                    if (currentLog != null)
                    {
                        if (exceptionDetails.Count > 0)
                        {
                            currentLog.Exception = string.Join(Environment.NewLine, exceptionDetails);
                        }
                        logs.Add(currentLog);
                        exceptionDetails.Clear();
                    }
                    
                    // Tạo log mới
                    currentLog = new SystemLog
                    {
                        Id = Guid.NewGuid().ToString(),
                        Timestamp = ParseDateTime(simpleMatch.Groups[1].Value),
                        Level = NormalizeLogLevel(simpleMatch.Groups[2].Value),
                        Message = simpleMatch.Groups[3].Value,
                        Source = fileName,
                        Details = line
                    };
                }
                else if (exceptionMatch.Success && currentLog != null)
                {
                    // Thêm thông tin exception vào log hiện tại
                    exceptionDetails.Add(line);
                    currentLog.Level = "error"; // Tự động nâng cấp level lên error nếu có exception
                }
                else if (stackTraceMatch && currentLog != null)
                {
                    // Đây là dòng stack trace, thêm vào chi tiết exception
                    exceptionDetails.Add(line);
                }
                else if (currentLog != null && !string.IsNullOrWhiteSpace(line))
                {
                    // Dòng tiếp theo của log hiện tại (thông tin bổ sung)
                    exceptionDetails.Add(line);
                }
            }
            
            // Thêm log cuối cùng vào danh sách
            if (currentLog != null)
            {
                if (exceptionDetails.Count > 0)
                {
                    currentLog.Exception = string.Join(Environment.NewLine, exceptionDetails);
                }
                logs.Add(currentLog);
            }
            
            return logs;
        }
        
        // Phương thức này giúp chuẩn hóa các mức độ log khác nhau
        private string NormalizeLogLevel(string level)
        {
            level = level.ToLower();
            
            // Chuẩn hóa các level về info, warning, error, debug
            if (level.Contains("info") || level.Contains("information"))
                return "info";
            if (level.Contains("warn") || level.Contains("warning"))
                return "warning";
            if (level.Contains("error") || level.Contains("err") || level.Contains("critical") || level.Contains("fatal"))
                return "error";
            if (level.Contains("debug") || level.Contains("dbug") || level.Contains("trace"))
                return "debug";
            
            return level;
        }
        
        // Hàm phân tích chuỗi thành DateTime với nhiều định dạng khác nhau
        private DateTime ParseDateTime(string dateTimeStr)
        {
            // Thử parse với định dạng có milliseconds
            if (DateTime.TryParseExact(dateTimeStr, "yyyy-MM-dd HH:mm:ss.fff", null, System.Globalization.DateTimeStyles.None, out var result))
            {
                return result;
            }
            
            // Thử parse với định dạng không có milliseconds
            if (DateTime.TryParseExact(dateTimeStr, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out result))
            {
                return result;
            }
            
            // Fallback: sử dụng DateTime.Parse
            try
            {
                return DateTime.Parse(dateTimeStr);
            }
            catch
            {
                return DateTime.Now;
            }
        }
    }
}