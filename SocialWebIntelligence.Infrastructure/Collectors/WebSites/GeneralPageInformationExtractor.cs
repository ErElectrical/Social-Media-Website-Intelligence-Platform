using HtmlAgilityPack;
using SocialWebIntelligence.Core.DTOs;
using System.Text.RegularExpressions;

namespace SocialWebIntelligence.Infrastructure.Collectors.Websites;

public class GeneralPageInformationExtractor
{
    public GeneralPageInformation Extract(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            throw new ArgumentException("HTML content cannot be empty.", nameof(html));

        var document = new HtmlDocument();
        document.LoadHtml(html);

        // Extract metadata before removing unwanted page elements.
        var pageTitle = CleanText(
            document.DocumentNode.SelectSingleNode("//title")?.InnerText);

        var metaDescription = CleanText(
            document.DocumentNode.SelectSingleNode(
                "//meta[translate(@name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='description']")
                ?.GetAttributeValue("content", null));

        // Remove elements that usually contain navigation or non-page content.
        var unwantedNodes = document.DocumentNode.SelectNodes(
            "//script | //style | //noscript | //svg | //nav | //header | //footer");

        if (unwantedNodes != null)
        {
            foreach (var node in unwantedNodes)
                node.Remove();
        }

        var mainHeading = CleanText(
            document.DocumentNode.SelectSingleNode("//h1")?.InnerText);

        var headings = ExtractTextList(
            document.DocumentNode.SelectNodes("//h2 | //h3"));

        var paragraphs = ExtractTextList(
            document.DocumentNode.SelectNodes("//p"));

        var listItems = ExtractTextList(
            document.DocumentNode.SelectNodes("//li"));

        return new GeneralPageInformation
        {
            PageTitle = pageTitle,
            MetaDescription = metaDescription,
            MainHeading = mainHeading,
            Headings = headings,
            DescriptionParagraphs = paragraphs,
            ListItems = listItems
        };
    }

    private static List<string> ExtractTextList(
        HtmlNodeCollection? nodes)
    {
        var results = new List<string>();

        if (nodes == null)
            return results;

        foreach (var node in nodes)
        {
            var text = CleanText(node.InnerText);

            if (!string.IsNullOrWhiteSpace(text) &&
                !results.Contains(text, StringComparer.OrdinalIgnoreCase))
            {
                results.Add(text);
            }
        }

        return results;
    }

    private static string? CleanText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = HtmlEntity.DeEntitize(value);

        return Regex.Replace(value, @"\s+", " ").Trim();
    }
}