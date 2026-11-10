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



    // TEST 6: All supported internal page types should be discovered.
    [Fact]
    public async Task DiscoverPagesAsync_ReturnsAllSupportedPageTypes()
    {
        // Arrange
        // Each link represents a different type of company information page.
        var html = """
            <html>
                <body>
                    <a href="/contact-us">Contact Us</a>
                    <a href="/about">About Us</a>
                    <a href="/products">Our Products</a>
                    <a href="/services">Our Services</a>
                    <a href="/blog">Company Blog</a>
                    <a href="/careers">Careers</a>
                </body>
            </html>
            """;

        var service = CreateService(html);

        // Act
        var result = await service.DiscoverPagesAsync(
            "https://example.com");

        // Assert
        // Verify both the page classification and its resolved absolute URL.
        Assert.Collection(
            result,
            page =>
            {
                Assert.Equal("Contact", page.PageType);
                Assert.Equal("https://example.com/contact-us", page.PageUrl);
            },
            page =>
            {
                Assert.Equal("About", page.PageType);
                Assert.Equal("https://example.com/about", page.PageUrl);
            },
            page =>
            {
                Assert.Equal("Products", page.PageType);
                Assert.Equal("https://example.com/products", page.PageUrl);
            },
            page =>
            {
                Assert.Equal("Services", page.PageType);
                Assert.Equal("https://example.com/services", page.PageUrl);
            },
            page =>
            {
                Assert.Equal("News", page.PageType);
                Assert.Equal("https://example.com/blog", page.PageUrl);
            },
            page =>
            {
                Assert.Equal("Careers", page.PageType);
                Assert.Equal("https://example.com/careers", page.PageUrl);
            });
    }

    // TEST 7: Links pointing to other domains must not be collected.
    [Fact]
    public async Task DiscoverPagesAsync_IgnoresExternalDomainLinks()
    {
        // Arrange
        var html = """
            <html>
                <body>
                    <a href="/about">About Us</a>
                    <a href="https://another-company.com/products">
                        Products
                    </a>
                </body>
            </html>
            """;

        var service = CreateService(html);

        // Act
        var result = await service.DiscoverPagesAsync(
            "https://example.com");

        // Assert
        // Only pages belonging to the registered website should be returned.
        var page = Assert.Single(result);

        Assert.Equal("About", page.PageType);
        Assert.Equal("https://example.com/about", page.PageUrl);
    }

    // TEST 8: The same page linked multiple times should appear only once.
    [Fact]
    public async Task DiscoverPagesAsync_RemovesDuplicateUrls()
    {
        // Arrange
        var html = """
            <html>
                <body>
                    <a href="/contact">Contact Us</a>
                    <a href="/contact">Contact</a>
                    <a href="/contact">Get in Touch</a>
                </body>
            </html>
            """;

        var service = CreateService(html);

        // Act
        var result = await service.DiscoverPagesAsync(
            "https://example.com");

        // Assert
        // Duplicate navigation links should not create duplicate page records.
        var page = Assert.Single(result);

        Assert.Equal("Contact", page.PageType);
        Assert.Equal("https://example.com/contact", page.PageUrl);
    }

    // TEST 9: Unsupported links and unrelated pages should be ignored.
    [Fact]
    public async Task DiscoverPagesAsync_IgnoresUnsupportedAndUnrecognizedLinks()
    {
        // Arrange
        var html = """
            <html>
                <body>
                    <a href="mailto:info@example.com">Email Us</a>
                    <a href="javascript:void(0)">Click Here</a>
                    <a href="/random-page">Read More</a>
                </body>
            </html>
            """;

        var service = CreateService(html);

        // Act
        var result = await service.DiscoverPagesAsync(
            "https://example.com");

        // Assert
        // None of these links represents a supported website page type.
        Assert.Empty(result);
    }

    // TEST 10: Invalid website URLs should be rejected.
    [Fact]
    public async Task DiscoverPagesAsync_ThrowsException_ForInvalidUrl()
    {
        // Arrange
        var service = CreateService("<html></html>");

        // Act and Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.DiscoverPagesAsync("invalid-url"));
    }

    // TEST 11: An unsuccessful homepage response should raise an exception.
    [Fact]
    public async Task DiscoverPagesAsync_ThrowsException_WhenWebsiteReturns404()
    {
        // Arrange
        var service = CreateService(
            "<html></html>",
            HttpStatusCode.NotFound);

        // Act and Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.DiscoverPagesAsync(
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
