using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialWebIntelligence.Core.Entities
{
    public class MonitoredSource
    {
        public int Id { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public SourcePlatform Platform { get; set; }

        public string Url { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastCollectedAt { get; set; }

    }
}
