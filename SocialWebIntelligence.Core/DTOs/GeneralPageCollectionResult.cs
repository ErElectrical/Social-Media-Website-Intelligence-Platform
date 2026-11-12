namespace SocialWebIntelligence.Core.DTOs;

public class GeneralPageCollectionResult
{
    public string PageUrl { get; set; } = string.Empty;

    public string? PageTitle { get; set; }

    public int HttpStatusCode { get; set; }

    public string ContentHash { get; set; } = string.Empty;

    public GeneralPageInformation Information { get; set; } = new();
}