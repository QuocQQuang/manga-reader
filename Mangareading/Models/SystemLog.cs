using System;

namespace Mangareading.Models
{
    public class SystemLog
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } // info, warning, error
        public string Message { get; set; }
        public string Source { get; set; }
        public string Exception { get; set; }
        public string Details { get; set; }
    }
}