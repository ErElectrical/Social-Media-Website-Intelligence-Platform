using SocialWebIntelligence.Infrastructure.Collectors.WebSites;
using System.Net;
using Xunit;

namespace SocialWebIntelligence.Tests;

public class WebsitePageDiscoveryTests
{
    // TEST 1: Contact page exists.
    [Fact]
    public async Task DiscoverContactPageAsync_ReturnsContactUrl()
    {
        // Arrange
        var html = """
            <html>
                <body>
                    <a href="/about">About Us</a>
                    <a href="/contact-us">Contact Us</a>
                </body>
            </html>
            """;

        var service = CreateService(html);

        // Act
        var result = await service.DiscoverContactPageAsync(
            "https://example.com");

        // Assert
        Assert.Equal(
            "https://example.com/contact-us",
            result);
    }

    // TEST 2: No Contact page exists.
    [Fact]
    public async Task DiscoverContactPageAsync_ReturnsNull_WhenContactLinkMissing()
    {
        // Arrange
        var html = """
            <html>
                <body>
                    <a href="/about">About Us</a>
                    <a href="/services">Services</a>
                </body>
            </html>
            """;

        var service = CreateService(html);

        // Act
        var result = await service.DiscoverContactPageAsync(
            "https://example.com");

        // Assert
        Assert.Null(result);
    }

    // TEST 3: External domain links must be ignored.
    [Fact]
    public async Task DiscoverContactPageAsync_IgnoresExternalDomain()
    {
        // Arrange
        var html = """
            <html>
                <body>
                    <a href="https://another-company.com/contact">
                        Contact Us
                    </a>
                </body>
            </html>
            """;

        var service = CreateService(html);

        // Act
        var result = await service.DiscoverContactPageAsync(
            "https://example.com");

        // Assert
        Assert.Null(result);
    }

    // TEST 4: Invalid website URL must be rejected.
    [Fact]
    public async Task DiscoverContactPageAsync_ThrowsException_ForInvalidUrl()
    {
        // Arrange
        var service = CreateService("<html></html>");

        // Act and Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.DiscoverContactPageAsync("invalid-url"));
    }

    // TEST 5: HTTP errors must be handled.
    [Fact]
    public async Task DiscoverContactPageAsync_ThrowsException_WhenWebsiteReturns404()
    {
        // Arrange
        var service = CreateService(
            "<html></html>",
            HttpStatusCode.NotFound);

        // Act and Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.DiscoverContactPageAsync(
                "https://example.com"));
    }

    // Creates a service with a simulated HTTP response.
    private static WebsitePageDiscoveryService CreateService(
        string html,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new FakeHttpMessageHandler(html, statusCode);

        var httpClient = new HttpClient(handler);

        return new WebsitePageDiscoveryService(httpClient);
    }

    // Simulates a website without making a real internet request.
    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _html;
        private readonly HttpStatusCode _statusCode;

        public FakeHttpMessageHandler(
            string html,
            HttpStatusCode statusCode)
        {
            _html = html;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_html)
            });
        }
    }
}
