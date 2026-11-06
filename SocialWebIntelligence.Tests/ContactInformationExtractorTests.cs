
using SocialWebIntelligence.Core.DTOs;
using SocialWebIntelligence.Infrastructure.Collectors.Websites;
using Xunit;

namespace SocialWebIntelligence.Tests;

public class ContactInformationExtractorTests
{
    private readonly ContactInformationExtractor _extractor = new();

    // TEST 1: Extract all four types of contact information.
    [Fact]
    public void Extract_ReturnsAllContactInformation()
    {
        // Arrange: Simulate a company Contact Us page.
        var html = """
            <html>
                <body>
                    <h2>Contact Us</h2>

                    <p>Email: info@jasch.example</p>
                    <p>Phone: +91 9876543210</p>

                    <address>
                        Bahadurgarh, Haryana, India
                    </address>

                    <a href="https://www.linkedin.com/company/jasch-example">
                        LinkedIn
                    </a>

                    <a href="https://www.facebook.com/jasch-example">
                        Facebook
                    </a>
                </body>
            </html>
            """;

        // Act: Extract contact information.
        ContactInformation result = _extractor.Extract(html);

        // Assert: Verify each extracted field.
        Assert.Contains("info@jasch.example", result.Emails);

        Assert.Contains("+91 9876543210", result.PhoneNumbers);

        Assert.Contains(
            "Bahadurgarh, Haryana, India",
            result.Addresses);

        Assert.Contains("LinkedIn", result.SocialLinks.Keys);
        Assert.Contains("Facebook", result.SocialLinks.Keys);
    }

    // TEST 2: Extract email addresses from mailto links.
    [Fact]
    public void Extract_FindsEmailFromMailtoLink()
    {
        // Arrange
        var html = """
            <html>
                <body>
                    <a href="mailto:sales@jasch.example">
                        Send Email
                    </a>
                </body>
            </html>
            """;

        // Act
        var result = _extractor.Extract(html);

        // Assert
        Assert.Contains("sales@jasch.example", result.Emails);
    }

    // TEST 3: Extract phone numbers from tel links.
    [Fact]
    public void Extract_FindsPhoneFromTelLink()
    {
        // Arrange
        var html = """
            <html>
                <body>
                    <a href="tel:+911234567890">
                        Call Us
                    </a>
                </body>
            </html>
            """;

        // Act
        var result = _extractor.Extract(html);

        // Assert
        Assert.Contains("+911234567890", result.PhoneNumbers);
    }

    // TEST 4: Avoid duplicate email addresses.
    [Fact]
    public void Extract_DoesNotAddDuplicateEmails()
    {
        // Arrange
        var html = """
            <html>
                <body>
                    <p>info@jasch.example</p>
                    <a href="mailto:info@jasch.example">
                        Email Us
                    </a>
                </body>
            </html>
            """;

        // Act
        var result = _extractor.Extract(html);

        // Assert  it verifies that the email collection contains exactly one item.
        Assert.Single(result.Emails);
    }

    // TEST 5: Return empty collections when no contact data exists.
    [Fact]
    public void Extract_ReturnsEmptyCollections_WhenNoContactDataExists()
    {
        // Arrange
        var html = """
            <html>
                <body>
                    <h1>Welcome to our company</h1>
                    <p>We provide industrial solutions.</p>
                </body>
            </html>
            """;

        // Act
        var result = _extractor.Extract(html);

        // Assert
        Assert.Empty(result.Emails);
        Assert.Empty(result.PhoneNumbers);
        Assert.Empty(result.Addresses);
        Assert.Empty(result.SocialLinks);
    }

    // TEST 6: Reject empty HTML input.
    [Fact]
    public void Extract_ThrowsException_WhenHtmlIsEmpty()
    {
        // Arrange
        var html = "";

        // Act and Assert
        Assert.Throws<ArgumentException>(
            () => _extractor.Extract(html));
    }
}
