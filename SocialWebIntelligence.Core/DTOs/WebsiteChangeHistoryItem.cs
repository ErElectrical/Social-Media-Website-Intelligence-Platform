
namespace SocialWebIntelligence.Core.DTOs;

public class WebsiteChangeHistoryItem
{
    public int ChangeId { get; set; }

    public int WebsiteCollectionId { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public string PageType { get; set; } = string.Empty;

    public string PageUrl { get; set; } = string.Empty;

    public string Field { get; set; } = string.Empty;

    public string ChangeType { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime DetectedAt { get; set; }
}
