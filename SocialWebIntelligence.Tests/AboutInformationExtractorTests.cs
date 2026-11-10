
using SocialWebIntelligence.Infrastructure.Collectors.Websites;
using Xunit;

namespace SocialWebIntelligence.Tests;

public class AboutInformationExtractorTests
{
    private readonly AboutInformationExtractor _extractor = new();

    // TEST 1: Extract the browser title.
    [Fact]
    public void Extract_ReturnsPageTitle()
    {
        var html = """
            <html>
                <head>
                    <title>About ABC Technologies</title>
                </head>
                <body></body>
            </html>
            """;

        var result = _extractor.Extract(html);

        Assert.Equal("About ABC Technologies", result.PageTitle);
    }

    // TEST 2: Extract the SEO meta description.
    [Fact]
    public void Extract_ReturnsMetaDescription()
    {
        var html = """
            <html>
                <head>
                    <meta name="description"
                          content="Industrial automation solutions.">
                </head>
                <body></body>
            </html>
            """;

        var result = _extractor.Extract(html);

        Assert.Equal(
            "Industrial automation solutions.",
            result.MetaDescription);
    }

    // TEST 3: Extract the first H1 as the main heading.
    [Fact]
    public void Extract_ReturnsMainHeading()
    {
        var html = """
            <html>
                <body>
                    <h1>Who We Are</h1>
                    <h1>Another Heading</h1>
                </body>
            </html>
            """;

        var result = _extractor.Extract(html);

        Assert.Equal("Who We Are", result.MainHeading);
    }

    // TEST 4: Extract H2 and H3 section headings.
    [Fact]
    public void Extract_ReturnsSectionHeadings()
    {
        var html = """
            <html>
                <body>
                    <h1>About Us</h1>
                    <h2>Our History</h2>
                    <h3>Our Vision</h3>
                    <h2>Our Mission</h2>
                </body>
            </html>
            """;

        var result = _extractor.Extract(html);

        Assert.Equal(
            new[] { "Our History", "Our Vision", "Our Mission" },
            result.Headings);
    }

    // TEST 5: Extract descriptive paragraphs.
    [Fact]
    public void Extract_ReturnsDescriptionParagraphs()
    {
        var html = """
            <html>
                <body>
                    <main>
                        <p>We provide industrial solutions.</p>
                        <p>We were established in 2005.</p>
                    </main>
                </body>
            </html>
            """;

        var result = _extractor.Extract(html);

        Assert.Equal(2, result.DescriptionParagraphs.Count);
        Assert.Contains(
            "We provide industrial solutions.",
            result.DescriptionParagraphs);
        Assert.Contains(
            "We were established in 2005.",
            result.DescriptionParagraphs);
    }

    // TEST 6: Ignore paragraphs inside navigation and footer.
    [Fact]
    public void Extract_IgnoresNavigationAndFooterParagraphs()
    {
        var html = """
            <html>
                <body>
                    <nav>
                        <p>Navigation text</p>
                    </nav>

                    <main>
                        <p>Company information.</p>
                    </main>

                    <footer>
                        <p>Copyright information.</p>
                    </footer>
                </body>
            </html>
            """;

        var result = _extractor.Extract(html);

        var paragraph = Assert.Single(result.DescriptionParagraphs);

        Assert.Equal("Company information.", paragraph);
    }

    // TEST 7: Remove duplicate headings and paragraphs.
    [Fact]
    public void Extract_RemovesDuplicateContent()
    {
        var html = """
            <html>
                <body>
                    <h2>Our History</h2>
                    <h2>Our History</h2>

                    <p>We build industrial systems.</p>
                    <p>We build industrial systems.</p>
                </body>
            </html>
            """;

        var result = _extractor.Extract(html);

        Assert.Single(result.Headings);
        Assert.Single(result.DescriptionParagraphs);
    }

    // TEST 8: Decode HTML entities.
    [Fact]
    public void Extract_DecodesHtmlEntities()
    {
        var html = """
            <html>
                <body>
                    <h1>Research &amp; Development</h1>
                    <p>We provide research &amp; development services.</p>
                </body>
            </html>
            """;

        var result = _extractor.Extract(html);

        Assert.Equal(
            "Research & Development",
            result.MainHeading);

        Assert.Contains(
            "We provide research & development services.",
            result.DescriptionParagraphs);
    }

    // TEST 9: Reject empty HTML input.
    [Fact]
    public void Extract_ThrowsException_ForEmptyHtml()
    {
        Assert.Throws<ArgumentException>(
            () => _extractor.Extract(""));
    }
}
