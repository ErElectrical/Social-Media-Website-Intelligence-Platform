
using HtmlAgilityPack;
using SocialWebIntelligence.Core.DTOs;
using SocialWebIntelligence.Core.Entities;
using SocialWebIntelligence.Infrastructure.Collectors.WebSites;
using System.Security.Cryptography;
using System.Text;

namespace SocialWebIntelligence.Infrastructure.Collectors.Websites;

/// <summary>
/// Discovers a Contact Us page, downloads its HTML,
/// and extracts publicly available contact information.
/// </summary>
public class WebsiteContactCollectionService
{
    private readonly HttpClient _httpClient;
    private readonly WebsitePageDiscoveryService _pageDiscoveryService;
    private readonly ContactInformationExtractor _contactInformationExtractor;

    public WebsiteContactCollectionService(
        HttpClient httpClient,
        WebsitePageDiscoveryService pageDiscoveryService,
        ContactInformationExtractor contactInformationExtractor)
    {
        _httpClient = httpClient;
        _pageDiscoveryService = pageDiscoveryService;
        _contactInformationExtractor = contactInformationExtractor;
    }

    public async Task<WebsiteContactCollectionResult?> CollectAsync(
        string websiteUrl,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Discover the Contact Us page.
        var contactPageUrl =
            await _pageDiscoveryService.DiscoverContactPageAsync(
                websiteUrl,
                cancellationToken);

        if (string.IsNullOrWhiteSpace(contactPageUrl))
            return null;

        // Step 2: Download the Contact Us page.
        using var response = await _httpClient.GetAsync(
            contactPageUrl,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(
            cancellationToken);

        // Step 3: Extract contact information.
        var contactInformation =
            _contactInformationExtractor.Extract(html);

        // Step 4: Read the page title.
        var document = new HtmlDocument();
        document.LoadHtml(html);

        var pageTitle = document.DocumentNode
            .SelectSingleNode("//title")
            ?.InnerText;

        pageTitle = pageTitle == null
            ? null
            : HtmlEntity.DeEntitize(pageTitle).Trim();

        // Step 5: Generate SHA256 hash of downloaded HTML.
        var contentBytes = Encoding.UTF8.GetBytes(html);

        var hashBytes = SHA256.HashData(contentBytes);

        var contentHash = Convert.ToHexString(hashBytes);

        // Step 6: Return extracted data and collection metadata.
        return new WebsiteContactCollectionResult
        {
            PageUrl = response.RequestMessage?.RequestUri?.AbsoluteUri
                      ?? contactPageUrl,

            PageTitle = pageTitle,

            HttpStatusCode = (int)response.StatusCode,

            ContentHash = contentHash,

            ContactInformation = contactInformation
        };
    }
}
