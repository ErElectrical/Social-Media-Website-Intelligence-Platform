
using SocialWebIntelligence.Infrastructure.Collectors.Websites;
using SocialWebIntelligence.Infrastructure.Collectors.WebSites;
using System.Net;
using Xunit;

namespace SocialWebIntelligence.Tests;

public class WebsiteContactCollectionServiceTests
{
    [Fact]
    public async Task CollectAsync_ReturnsExtractedContactInformation()
    {
        // Arrange: HTML returned by the company homepage.
        var homePageHtml = """
            <html>
                <body>
                    <h1>Example Company</h1>
                    <a href="/contact-us">Contact Us</a>
                </body>
            </html>
            """;

        // Arrange: HTML returned by the Contact Us page.
        var contactPageHtml = """
            <html>
                <body>
                    <h1>Contact Us</h1>
                    <p>Email: info@example.com</p>
                    <p>Phone: +91 9876543210</p>
                    <address>Bahadurgarh, Haryana, India</address>
                    <a href="https://www.linkedin.com/company/example">
                        LinkedIn
                    </a>
                </body>
            </html>
            """;

        // Create HTTP clients with fake responses.
        var discoveryHttpClient = new HttpClient(
            new FakeHttpMessageHandler(homePageHtml));

        var collectionHttpClient = new HttpClient(
            new FakeHttpMessageHandler(contactPageHtml));

        // Create the required dependencies.
        var discoveryService =
            new WebsitePageDiscoveryService(discoveryHttpClient);

        var extractor = new ContactInformationExtractor();

        var collectionService = new WebsiteContactCollectionService(
            collectionHttpClient,
            discoveryService,
            extractor);

        // Act: Execute the actual collection service.
        var result = await collectionService.CollectAsync(
            "https://example.com");

        // Assert: Verify extracted information.
        Assert.NotNull(result);

        Assert.Contains("info@example.com", result.ContactInformation.Emails);

        Assert.Contains(
            "+91 9876543210",
            result.ContactInformation.PhoneNumbers);

        Assert.Contains(
            "Bahadurgarh, Haryana, India",
            result.ContactInformation.Addresses);

        Assert.Contains("LinkedIn", result.ContactInformation.SocialLinks.Keys);
    }

    [Fact]
    public async Task CollectAsync_ReturnsNull_WhenContactPageIsNotFound()
    {
        // Homepage without a Contact Us link.
        var homePageHtml = """
            <html>
                <body>
                    <h1>Example Company</h1>
                    <a href="/about">About Us</a>
                </body>
            </html>
            """;

        var discoveryHttpClient = new HttpClient(
            new FakeHttpMessageHandler(homePageHtml));

        var collectionHttpClient = new HttpClient(
            new FakeHttpMessageHandler("<html></html>"));

        var discoveryService =
            new WebsitePageDiscoveryService(discoveryHttpClient);

        var extractor = new ContactInformationExtractor();

        var collectionService = new WebsiteContactCollectionService(
            collectionHttpClient,
            discoveryService,
            extractor);

        // Act
        var result = await collectionService.CollectAsync(
            "https://example.com");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Returns predefined HTML instead of making real HTTP requests.
    /// </summary>
    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _html;

        public FakeHttpMessageHandler(string html)
        {
            _html = html;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(
                HttpStatusCode.OK)
            {
                Content = new StringContent(_html)
            });
        }
    }
}
