using SocialWebIntelligence.Core.DTOs;
using SocialWebIntelligence.Infrastructure.Collectors.Websites;

namespace SocialWebIntelligence.Tests;

public class GeneralPageInformationExtractorTests
{
    private readonly GeneralPageInformationExtractor _extractor = new();

    [Fact]
    public void Extract_ValidHtml_ReturnsAllExpectedInformation()
    {
        // Arrange
        var html = """
            <!DOCTYPE html>
            <html>
            <head>
                <title>Test Company Services</title>
                <meta name="description"
                      content="Engineering and automation services.">
            </head>
            <body>
                <nav>
                    <a href="/">Home</a>
                </nav>

                <h1>Our Services</h1>

                <h2>Industrial Automation</h2>
                <p>We provide industrial automation solutions.</p>

                <h2>Software Development</h2>
                <p>We develop custom software applications.</p>

                <ul>
                    <li>PLC Programming</li>
                    <li>SCADA Development</li>
                </ul>

                <footer>Copyright Test Company</footer>
            </body>
            </html>
            """;

        // Act
        var result = _extractor.Extract(html);

        // Assert
        Assert.Equal("Test Company Services", result.PageTitle);
        Assert.Equal(
            "Engineering and automation services.",
            result.MetaDescription);

        Assert.Equal("Our Services", result.MainHeading);

        Assert.Equal(2, result.Headings.Count);
        Assert.Contains("Industrial Automation", result.Headings);
        Assert.Contains("Software Development", result.Headings);

        Assert.Equal(2, result.DescriptionParagraphs.Count);
        Assert.Contains(
            "We provide industrial automation solutions.",
            result.DescriptionParagraphs);

        Assert.Equal(2, result.ListItems.Count);
        Assert.Contains("PLC Programming", result.ListItems);
        Assert.Contains("SCADA Development", result.ListItems);
    }

    [Fact]
    public void Extract_RemovesDuplicateHeadingsParagraphsAndListItems()
    {
        // Arrange
        var html = """
            <html>
            <body>
                <h1>Products</h1>

                <h2>Industrial Sensors</h2>
                <h2>Industrial Sensors</h2>

                <p>High quality sensors.</p>
                <p>High quality sensors.</p>

                <ul>
                    <li>Temperature Monitoring</li>
                    <li>Temperature Monitoring</li>
                </ul>
            </body>
            </html>
            """;

        // Act
        var result = _extractor.Extract(html);

        // Assert
        Assert.Single(result.Headings);
        Assert.Single(result.DescriptionParagraphs);
        Assert.Single(result.ListItems);
    }

    [Fact]
    public void Extract_RemovesNavigationAndUnwantedElements()
    {
        // Arrange
        var html = """
            <html>
            <body>
                <nav>
                    <h2>Navigation Heading</h2>
                    <p>Navigation paragraph</p>
                    <li>Navigation item</li>
                </nav>

                <script>
                    var message = "Script content";
                </script>

                <h1>Our Products</h1>
                <h2>Product Categories</h2>
                <p>Explore our products.</p>
                <ul>
                    <li>Product A</li>
                </ul>

                <footer>
                    <p>Footer content</p>
                </footer>
            </body>
            </html>
            """;

        // Act
        var result = _extractor.Extract(html);

        // Assert
        Assert.Equal("Our Products", result.MainHeading);

        Assert.DoesNotContain("Navigation Heading", result.Headings);
        Assert.DoesNotContain(
            "Navigation paragraph",
            result.DescriptionParagraphs);

        Assert.DoesNotContain(
            "Footer content",
            result.DescriptionParagraphs);

        Assert.DoesNotContain(
            "Navigation item",
            result.ListItems);
    }

    [Fact]
    public void Extract_EmptyHtml_ThrowsArgumentException()
    {
        // Arrange
        var html = string.Empty;

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => _extractor.Extract(html));
    }

    [Fact]
    public void Extract_DecodesHtmlEntitiesAndCleansWhitespace()
    {
        // Arrange
        var html = """
            <html>
            <body>
                <h1>Products &amp; Services</h1>
                <p>Industrial&nbsp;&nbsp;automation &amp; control.</p>
            </body>
            </html>
            """;

        // Act
        var result = _extractor.Extract(html);

        // Assert
        Assert.Equal("Products & Services", result.MainHeading);

        Assert.Contains(
            "Industrial automation & control.",
            result.DescriptionParagraphs);
    }
}