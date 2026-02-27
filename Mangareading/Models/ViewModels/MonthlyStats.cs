using System;
using System.ComponentModel.DataAnnotations;

namespace Mangareading.Models
{
    public class MonthlyStats
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public DateTime Month { get; set; }
        
        public int NewUsers { get; set; }
        
        public int NewManga { get; set; }
        
        public int NewChapters { get; set; }
        
        public long TotalViews { get; set; }
        
        public int UniqueVisitors { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
    }
} 