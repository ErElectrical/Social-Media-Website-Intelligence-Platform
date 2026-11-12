
using System.Net;
using System.Text;
using SocialWebIntelligence.Infrastructure.Collectors.Websites;

namespace SocialWebIntelligence.Tests;

public class GeneralPageCollectionServiceTests
{
    private static GeneralPageCollectionService CreateService(
        string html,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        // Arrange a fake HTTP response so tests don't depend on
        // an actual website or external network connection.
        var handler = new FakeHttpMessageHandler(
            (request, cancellationToken) =>
            {
                var response = new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(
                        html,
                        Encoding.UTF8,
                        "text/html")
                };

                return Task.FromResult(response);
            });

        // Use the real extractor to test the integration between
        // HTML downloading and structured information extraction.
        var httpClient = new HttpClient(handler);
        var extractor = new GeneralPageInformationExtractor();

        return new GeneralPageCollectionService(httpClient, extractor);
    }

    [Fact]
    public async Task CollectAsync_ValidPage_ReturnsExpectedInformation()
    {
        // Arrange: Prepare a sample Products page containing
        // metadata, headings, paragraphs and list items.
        var html = """
            <html>
            <head>
                <title>Test Company Products</title>
                <meta name="description"
                      content="Our industrial products.">
            </head>
            <body>
                <h1>Our Products</h1>
                <h2>Thickness Gauge</h2>
                <p>Measures material thickness accurately.</p>
                <ul>
                    <li>High accuracy</li>
                    <li>Real-time measurement</li>
                </ul>
            </body>
            </html>
            """;

        var service = CreateService(html);

        // Act: Collect the page and extract its structured information.
        var result = await service.CollectAsync(
            "https://example.com/products");

        // Assert: Verify that the collection service returns
        // the expected URL, HTTP status and extracted page information.
        Assert.Equal("Test Company Products", result.PageTitle);
        Assert.Equal(
            "https://example.com/products",
            result.PageUrl);
        Assert.Equal(200, result.HttpStatusCode);

        // The content hash must be generated successfully.
        Assert.False(string.IsNullOrWhiteSpace(result.ContentHash));

        // Verify that the extractor correctly populated the DTO.
        Assert.Equal("Our Products", result.Information.MainHeading);
        Assert.Contains(
            "Thickness Gauge",
            result.Information.Headings);
        Assert.Contains(
            "Measures material thickness accurately.",
            result.Information.DescriptionParagraphs);

        Assert.Equal(2, result.Information.ListItems.Count);
    }

    [Fact]
    public async Task CollectAsync_InvalidUrl_ThrowsArgumentException()
    {
        // Arrange: Create a service with a fake HTTP handler.
        // No HTTP request should be made for an invalid URL.
        var service = CreateService("<html></html>");

        // Act & Assert: The service must reject URLs that are not
        // valid absolute HTTP or HTTPS addresses.
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CollectAsync("invalid-url"));
    }

    [Fact]
    public async Task CollectAsync_HttpError_ThrowsHttpRequestException()
    {
        // Arrange: Simulate a website returning HTTP 404 Not Found.
        var service = CreateService(
            "<html>Not found</html>",
            HttpStatusCode.NotFound);

        // Act & Assert: EnsureSuccessStatusCode() should cause
        // the collection operation to throw HttpRequestException.
        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.CollectAsync(
                "https://example.com/products"));
    }

    [Fact]
    public async Task CollectAsync_UnchangedContent_ReturnsSameContentHash()
    {
        // Arrange: Use identical HTML for two collection attempts.
        // The service should extract the same structured information.
        var html = """
            <html>
            <head>
                <title>Test Products</title>
            </head>
            <body>
                <h1>Our Products</h1>
                <p>Industrial automation products.</p>
            </body>
            </html>
            """;

        var service = CreateService(html);

        // Act: Collect the same page twice.
        var firstResult = await service.CollectAsync(
            "https://example.com/products");

        var secondResult = await service.CollectAsync(
            "https://example.com/products");

        // Assert: Identical extracted information should produce
        // identical SHA-256 content hashes.
        Assert.Equal(
            firstResult.ContentHash,
            secondResult.ContentHash);
    }

    [Fact]
    public async Task CollectAsync_ChangedContent_ReturnsDifferentContentHash()
    {
        // Arrange: Prepare two versions of the same page.
        // Only the product description changes between versions.
        var htmlVersion1 = """
            <html>
            <body>
                <h1>Our Products</h1>
                <p>Industrial automation products.</p>
            </body>
            </html>
            """;

        var htmlVersion2 = """
            <html>
            <body>
                <h1>Our Products</h1>
                <p>Advanced industrial automation products.</p>
            </body>
            </html>
            """;

        // Separate services simulate two collection attempts
        // against the same URL at different points in time.
        var service1 = CreateService(htmlVersion1);
        var service2 = CreateService(htmlVersion2);

        // Act: Collect both versions of the page.
        var firstResult = await service1.CollectAsync(
            "https://example.com/products");

        var secondResult = await service2.CollectAsync(
            "https://example.com/products");

        // Assert: A meaningful change in extracted content
        // must produce a different content hash.
        Assert.NotEqual(
            firstResult.ContentHash,
            secondResult.ContentHash);
    }
}
