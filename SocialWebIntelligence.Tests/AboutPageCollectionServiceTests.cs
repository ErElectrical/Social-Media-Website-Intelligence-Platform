
using SocialWebIntelligence.Infrastructure.Collectors.Websites;
using System.Net;
using Xunit;

namespace SocialWebIntelligence.Tests;

public class AboutPageCollectionServiceTests
{
    // TEST 1: Successfully download and extract About page information.
    [Fact]
    public async Task CollectAsync_ReturnsExtractedAboutInformation()
    {
        // Arrange
        var html = """
            <html>
                <head>
                    <title>About ABC Technologies</title>
                    <meta name="description"
                          content="Industrial automation company.">
                </head>
                <body>
                    <h1>Who We Are</h1>
                    <h2>Our Vision</h2>
                    <p>We provide industrial automation solutions.</p>
                </body>
            </html>
            """;

        var service = CreateService(html);

        // Act
        var result = await service.CollectAsync(
            "https://example.com/about");

        // Assert
        Assert.Equal(
            "https://example.com/about",
            result.PageUrl);

        Assert.Equal(200, result.HttpStatusCode);

        Assert.Equal(
            "About ABC Technologies",
            result.PageTitle);

        Assert.Equal(
            "Industrial automation company.",
            result.AboutInformation.MetaDescription);

        Assert.Equal(
            "Who We Are",
            result.AboutInformation.MainHeading);

        Assert.Contains(
            "Our Vision",
            result.AboutInformation.Headings);

        Assert.Contains(
            "We provide industrial automation solutions.",
            result.AboutInformation.DescriptionParagraphs);
    }

    // TEST 2: HTTP errors should prevent extraction.
    [Fact]
    public async Task CollectAsync_ThrowsException_WhenWebsiteReturns404()
    {
        // Arrange
        var service = CreateService(
            "<html></html>",
            HttpStatusCode.NotFound);

        // Act and Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.CollectAsync(
                "https://example.com/about"));
    }

    // TEST 3: Invalid URLs must be rejected before HTTP execution.
    [Theory]
    [InlineData("invalid-url")]
    [InlineData("ftp://example.com/about")]
    [InlineData("")]
    public async Task CollectAsync_ThrowsException_ForInvalidUrl(
        string pageUrl)
    {
        // Arrange
        var service = CreateService("<html></html>");

        // Act and Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CollectAsync(pageUrl));
    }

    // Creates the collector using a simulated HTTP response.
    // This keeps tests independent of real websites and internet access.
    private static AboutPageCollectionService CreateService(
        string html,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new FakeHttpMessageHandler(html, statusCode);
        var httpClient = new HttpClient(handler);

        var extractor = new AboutInformationExtractor();

        return new AboutPageCollectionService(
            httpClient,
            extractor);
    }

    // Simulates the HTTP server response without making a network request.
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
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
                Content = new StringContent(_html),
                RequestMessage = request
            });
        }
    }
}
