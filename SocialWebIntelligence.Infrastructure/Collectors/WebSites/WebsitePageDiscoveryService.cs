using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace SocialWebIntelligence.Infrastructure.Collectors.WebSites
{
    public class WebsitePageDiscoveryService
    {
        private readonly HttpClient _httpClient;

        public WebsitePageDiscoveryService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<DiscoverWebsitePage>> DiscoverPagesAsync(
            string websiteUrl,
            CancellationToken cancellationToken = default
            )
        {

            if (!Uri.TryCreate(websiteUrl, UriKind.Absolute, out var baseUri) ||
                (baseUri.Scheme != Uri.UriSchemeHttp &&
                 baseUri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("Invalid website URL.");
            }

            var response = await _httpClient.GetAsync(
                baseUri
                , cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Website request failed. URL: {baseUri} | " +
                    $"Status: {(int)response.StatusCode} {response.ReasonPhrase}"
                );
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var document = new HtmlDocument();

            
            document.Load(html);
            var links = document.DocumentNode.SelectNodes("//a[@href]");

            var discoveredPages = new List<DiscoverWebsitePage>();

            var visitedUrls = new HashSet<string>(
                                StringComparer.OrdinalIgnoreCase);

            if(links == null)
            {
                return discoveredPages;
            }

            foreach(var link in links)
            {
                var href = link.GetAttributeValue("href", "").Trim();
                if (string.IsNullOrWhiteSpace(href))
                    continue;
                if (!Uri.TryCreate(baseUri, href, out var pageUri))
                    continue;
                if (pageUri.Scheme != Uri.UriSchemeHttp &&
                    pageUri.Scheme != Uri.UriSchemeHttps)
                    continue;

                // Restrict discovery to the same website host.
                if (!string.Equals(
                    pageUri.Host,
                    baseUri.Host,
                    StringComparison.OrdinalIgnoreCase))
                    continue;

                // Remove URL fragments before duplicate checking.
                var normalizedUrl = new UriBuilder(pageUri)
                {
                    Fragment = string.Empty
                }.Uri.AbsoluteUri;

                

                var linkText = HtmlEntity.DeEntitize(
                    link.InnerText ?? "").Trim();

                var searchText = $"{linkText} {pageUri.AbsolutePath}"
                .ToLowerInvariant();

                var PageType = IdentifyPageType(searchText);

                if (PageType == null)
                    continue;

                if (!visitedUrls.Add(normalizedUrl))
                    continue;

                discoveredPages.Add(new DiscoverWebsitePage(
                    PageType,
                    normalizedUrl));



            }

            return discoveredPages;
        }

        private static string? IdentifyPageType(string searchText)
        {
            if (ContainsAny(searchText,
                "contact", "get-in-touch", "get in touch",
                "reach-us", "reach us"))
                return "Contact";

            if (ContainsAny(searchText,
                "about", "our-story", "company"))
                return "About";

            if (ContainsAny(searchText,
                "products", "product"))
                return "Products";

            if (ContainsAny(searchText,
                "services", "service"))
                return "Services";

            if (ContainsAny(searchText,
                "news", "blog", "press", "articles"))
                return "News";

            if (ContainsAny(searchText,
                "careers", "career", "jobs", "job-openings"))
                return "Careers";

            return null;
        }

        private static bool ContainsAny(
            string text,
            params string[] keywords)
        {
            return keywords.Any(keyword =>
                text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
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

    public sealed record DiscoverWebsitePage(


        string PageType,
        string PageUrl


     );
}
