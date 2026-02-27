using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace Mangareading.Services
{
    /// <summary>
    /// Provider ghi log vào file
    /// </summary>
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _path;
        private readonly ConcurrentDictionary<string, FileLogger> _loggers = new ConcurrentDictionary<string, FileLogger>();

        public FileLoggerProvider(string path)
        {
            _path = path;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _path));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }

        private class FileLogger : ILogger
        {
            private readonly string _categoryName;
            private readonly string _path;
            private static readonly object _lock = new object();

            public FileLogger(string categoryName, string path)
            {
                _categoryName = categoryName;
                _path = path;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return logLevel != LogLevel.None;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }

                var message = formatter(state, exception);
                if (string.IsNullOrEmpty(message) && exception == null)
                {
                    return;
                }

                var logEntry = new StringBuilder();
                logEntry.Append($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ");
                logEntry.Append($"{logLevel} ");
                logEntry.Append($"[{_categoryName}] ");
                logEntry.AppendLine(message);

                if (exception != null)
                {
                    logEntry.AppendLine($"Exception: {exception}");
                }

                lock (_lock)
                {
                    try
                    {
                        File.AppendAllText(_path, logEntry.ToString());
                    }
                    catch
                    {
                        // Lỗi khi ghi file log không nên làm dừng ứng dụng
                        // Có thể xem xét thêm cơ chế retry hoặc ghi vào console trong trường hợp này
                    }
                }
            }
        }
    }
}