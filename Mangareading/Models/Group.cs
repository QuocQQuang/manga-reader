using System;
using System.Collections.Generic;

namespace Mangareading.Models
{
    public class Group
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<MangaGroup> MangaGroups { get; set; }
    }
}