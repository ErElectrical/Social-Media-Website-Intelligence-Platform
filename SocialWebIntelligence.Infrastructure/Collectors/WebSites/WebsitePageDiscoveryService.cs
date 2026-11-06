using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace SocialWebIntelligence.Infrastructure.Collectors.WebSites
{
    public class WebsitePageDiscoveryService
    {
        private readonly HttpClient _httpClient;

        public WebsitePageDiscoveryService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string?> DiscoverContactPageAsync(
            string websiteUrl,
            CancellationToken cancellationToken = default)
        {
            if (!Uri.TryCreate(websiteUrl, UriKind.Absolute, out var baseUri) ||
                (baseUri.Scheme != Uri.UriSchemeHttp &&
                 baseUri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("Invalid website URL.");
            }

            var response = await _httpClient.GetAsync(
                baseUri,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                throw new HttpRequestException(
                    $"Website request failed. " +
                    $"URL: {baseUri} | " +
                    $"Status: {(int)response.StatusCode} " +
                    $"{response.ReasonPhrase} | " +
                    $"Response: {errorContent[..Math.Min(errorContent.Length, 500)]}");
            }

            var html = await response.Content.ReadAsStringAsync(
                cancellationToken);

            var document = new HtmlDocument();
            document.LoadHtml(html);

            var links = document.DocumentNode.SelectNodes("//a[@href]");

            if (links == null)
                return null;

            foreach (var link in links)
            {
                var href = link.GetAttributeValue("href", "").Trim();

                var linkText = HtmlEntity.DeEntitize(
                    link.InnerText ?? "").Trim();

                var searchText = $"{linkText} {href}".ToLowerInvariant();

                bool isContactLink =
                    searchText.Contains("contact") ||
                    searchText.Contains("get-in-touch") ||
                    searchText.Contains("get in touch") ||
                    searchText.Contains("reach-us") ||
                    searchText.Contains("reach us");

                if (!isContactLink)
                    continue;

                if (!Uri.TryCreate(baseUri, href, out var contactUri))
                    continue;

                if (contactUri.Scheme != Uri.UriSchemeHttp &&
                    contactUri.Scheme != Uri.UriSchemeHttps)
                    continue;

                // Only consider links belonging to the same website.
                if (!string.Equals(
                    contactUri.Host,
                    baseUri.Host,
                    StringComparison.OrdinalIgnoreCase))
                    continue;

                return contactUri.AbsoluteUri;
            }

            return null;
        }
    }
}
