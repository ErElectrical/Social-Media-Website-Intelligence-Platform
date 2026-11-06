using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialWebIntelligence.Core.Entities
{
    public class WebsiteCollection
    {
        public int Id { get; set; }

        public int MonitoredSourceId { get; set; }

        public string PageUrl { get; set; } = string.Empty;

        public string? PageTitle { get; set; }

        public string PageType { get; set; } = "General";

        public string? ExtractedDataJson { get; set; }

        public string? ContentHash { get; set; }

        public int? HttpStatusCode { get; set; }

        public bool IsSuccessful { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
    }
}
