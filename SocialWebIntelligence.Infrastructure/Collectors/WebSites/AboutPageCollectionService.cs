
using Microsoft.VisualBasic;
using SocialWebIntelligence.Core.DTOs;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SocialWebIntelligence.Infrastructure.Collectors.Websites;

/// <summary>
/// Downloads an About page and extracts structured company information.
/// This service coordinates HTTP downloading and extraction but does
/// not discover URLs or persist information in the database.
/// </summary>
public class AboutPageCollectionService
{
    private readonly HttpClient _httpClient;
    private readonly AboutInformationExtractor _aboutInformationExtractor;

    public AboutPageCollectionService(
        HttpClient httpClient,
        AboutInformationExtractor aboutInformationExtractor)
    {
        _httpClient = httpClient;
        _aboutInformationExtractor = aboutInformationExtractor;
    }

    /// <summary>
    /// Downloads the supplied About page URL and extracts company information.
    /// </summary>
    public async Task<AboutPageCollectionResult> CollectAsync(
        string pageUrl,
        CancellationToken cancellationToken = default)
    {
        // Validate the URL before making an HTTP request.
        // Only HTTP and HTTPS pages are supported by this collector.
        if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "A valid HTTP or HTTPS page URL is required.",
                nameof(pageUrl));
        }

        // Download the page. HttpClient handles configured redirects
        // and cancellation through the supplied cancellation token.
        using var response = await _httpClient.GetAsync(
            uri,
            cancellationToken);

        // Do not attempt extraction when the website returns an HTTP error.
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(
            cancellationToken);

        // Convert the downloaded HTML into structured About information.
        var aboutInformation = _aboutInformationExtractor.Extract(html);

        // Hash the structured information instead of raw HTML.
        // This avoids treating irrelevant HTML changes as company-information changes.
        var serializedInformation = JsonSerializer.Serialize(
    aboutInformation,
    new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });

        var contentHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(serializedInformation)));

        // Return the extracted content together with HTTP metadata.
        return new AboutPageCollectionResult
        {
            // Prefer the final response URL because the website may redirect.
            PageUrl = response.RequestMessage?.RequestUri?.AbsoluteUri
                      ?? uri.AbsoluteUri,

            PageTitle = aboutInformation.PageTitle,

            HttpStatusCode = (int)response.StatusCode,

            AboutInformation = aboutInformation,

            ContentHash = contentHash

            
        };
    }
}
