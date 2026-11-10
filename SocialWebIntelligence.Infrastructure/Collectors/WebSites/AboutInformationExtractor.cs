
using HtmlAgilityPack;
using SocialWebIntelligence.Core.DTOs;
using System.Text.RegularExpressions;

namespace SocialWebIntelligence.Infrastructure.Collectors.Websites;

/// <summary>
/// Extracts company-related information from an About page.
/// This class only parses HTML; downloading and database persistence
/// are handled by separate services.
/// </summary>
public class AboutInformationExtractor
{
    /// <summary>
    /// Extracts metadata, headings and descriptive paragraphs from HTML.
    /// </summary>
    public AboutInformation Extract(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            throw new ArgumentException(
                "HTML content cannot be empty.",
                nameof(html));
        }

        var document = new HtmlDocument();
        document.LoadHtml(html);

        var result = new AboutInformation();

        // Extract the browser page title.
        result.PageTitle = CleanText(
            document.DocumentNode
                .SelectSingleNode("//title")
                ?.InnerText);

        // Extract the SEO description, which can provide a concise
        // summary even when the visible page content is extensive.
        var metaDescription = document.DocumentNode.SelectSingleNode(
            "//meta[translate(@name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ'," +
            "'abcdefghijklmnopqrstuvwxyz')='description']");

        result.MetaDescription = CleanText(
            metaDescription?.GetAttributeValue("content", null));

        // The first H1 is generally the primary heading of the page.
        result.MainHeading = CleanText(
            document.DocumentNode
                .SelectSingleNode("//h1")
                ?.InnerText);

        // H2 and H3 elements usually represent sections such as
        // Company History, Mission, Vision and Our Values.
        var headingNodes = document.DocumentNode.SelectNodes("//h2 | //h3");

        if (headingNodes != null)
        {
            foreach (var node in headingNodes)
            {
                AddUnique(
                    result.Headings,
                    CleanText(node.InnerText));
            }
        }

        // Exclude navigation, header and footer paragraphs because
        // they usually contain menus, copyright text or unrelated links.
        var paragraphNodes = document.DocumentNode.SelectNodes(
            "//p[not(ancestor::nav) and " +
            "not(ancestor::header) and " +
            "not(ancestor::footer)]");

        if (paragraphNodes != null)
        {
            foreach (var node in paragraphNodes)
            {
                var paragraph = CleanText(node.InnerText);

                AddUnique(
                    result.DescriptionParagraphs,
                    paragraph);
            }
        }

        return result;
    }

    /// <summary>
    /// Decodes HTML entities and normalizes whitespace so that
    /// extracted values are consistent and easier to compare.
    /// </summary>
    private static string? CleanText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var decodedText = HtmlEntity.DeEntitize(value);

        return Regex.Replace(
            decodedText,
            @"\s+",
            " ").Trim();
    }

    /// <summary>
    /// Prevents repeated headings and paragraphs from appearing
    /// multiple times in the extracted result.
    /// </summary>
    private static void AddUnique(
        List<string> destination,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (!destination.Contains(
            value,
            StringComparer.OrdinalIgnoreCase))
        {
            destination.Add(value);
        }
    }
}
