
namespace SocialWebIntelligence.Core.DTOs;

/// <summary>
/// Contains the extracted contact information and metadata
/// about the website page from which it was collected.
/// </summary>
public class WebsiteContactCollectionResult
{
    public string PageUrl { get; set; } = string.Empty;

    public string? PageTitle { get; set; }

    public int HttpStatusCode { get; set; }

    public string ContentHash { get; set; } = string.Empty;

    public ContactInformation ContactInformation { get; set; } = new();
}
