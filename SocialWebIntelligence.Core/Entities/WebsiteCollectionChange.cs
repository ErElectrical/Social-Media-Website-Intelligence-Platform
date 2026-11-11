namespace SocialWebIntelligence.Core.Entities;

public class WebsiteCollectionChange
{
    public int Id { get; set; }

    // Identifies the snapshot in which this change was detected.
    public int WebsiteCollectionId { get; set; }

    public string Field { get; set; } = string.Empty;

    // Possible values: Added, Removed, Modified.
    public string ChangeType { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}