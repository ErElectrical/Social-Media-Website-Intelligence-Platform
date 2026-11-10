

namespace SocialWebIntelligence.Core.DTOs;

/// <summary>
/// Contains extracted About page information along with
/// metadata describing the HTTP collection operation.
/// </summary>
public class AboutPageCollectionResult
{
    // Final URL after HTTP redirects, when available.
    public string PageUrl { get; set; } = string.Empty;

    // Title of the downloaded About page.
    public string? PageTitle { get; set; }

    // HTTP response status received from the website.
    public int HttpStatusCode { get; set; }

    // Hash of the extracted About information, used for change detection.
    public string ContentHash { get; set; } = string.Empty;

    // Structured company information extracted from the page.
    public AboutInformation AboutInformation { get; set; } = new();
}
