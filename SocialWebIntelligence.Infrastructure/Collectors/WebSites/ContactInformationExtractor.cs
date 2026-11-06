
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using SocialWebIntelligence.Core.DTOs;

namespace SocialWebIntelligence.Infrastructure.Collectors.Websites;

/// <summary>
/// Extracts publicly displayed contact information from website HTML.
/// This class does not download websites or save anything to the database.
/// </summary>
public class ContactInformationExtractor
{
    // Matches common email address formats.
    private static readonly Regex EmailRegex = new(
        @"[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Finds potential phone numbers. Candidates are filtered by digit count.
    private static readonly Regex PhoneRegex = new(
        @"(?<!\w)\+?\d[\d\s().\-]{5,}\d(?!\w)",
        RegexOptions.Compiled);

    /// <summary>
    /// Extracts contact information from an HTML document.
    /// </summary>
    public ContactInformation Extract(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            throw new ArgumentException("HTML content cannot be empty.", nameof(html));

        var result = new ContactInformation();

        var document = new HtmlDocument();
        document.LoadHtml(html);

        ExtractEmails(document, result);
        ExtractPhoneNumbers(document, result);
        ExtractAddresses(document, result);
        ExtractSocialLinks(document, result);

        return result;
    }

    /// <summary>
    /// Finds email addresses in visible page text and mailto links.
    /// </summary>
    private static void ExtractEmails(
        HtmlDocument document,
        ContactInformation result)
    {
        var pageText = HtmlEntity.DeEntitize(
            document.DocumentNode.InnerText ?? string.Empty);

        AddUnique(
            result.Emails,
            EmailRegex.Matches(pageText)
                .Select(match => match.Value.Trim()));

        var emailLinks = document.DocumentNode.SelectNodes(
            "//a[starts-with(translate(@href,'MAILTO','mailto'),'mailto:')]");

        if (emailLinks == null)
            return;

        foreach (var link in emailLinks)
        {
            var href = link.GetAttributeValue("href", "");
            var email = href["mailto:".Length..].Split('?')[0].Trim();

            if (EmailRegex.IsMatch(email))
                AddUnique(result.Emails, new[] { email });
        }
    }

    /// <summary>
    /// Finds potential phone numbers in page text and telephone links.
    /// </summary>
    private static void ExtractPhoneNumbers(
        HtmlDocument document,
        ContactInformation result)
    {
        var pageText = HtmlEntity.DeEntitize(
            document.DocumentNode.InnerText ?? string.Empty);

        var candidates = PhoneRegex.Matches(pageText)
            .Select(match => match.Value);

        AddValidPhoneNumbers(result, candidates);

        var phoneLinks = document.DocumentNode.SelectNodes(
            "//a[starts-with(translate(@href,'TEL','tel'),'tel:')]");

        if (phoneLinks == null)
            return;

        foreach (var link in phoneLinks)
        {
            var href = link.GetAttributeValue("href", "");
            var phone = href["tel:".Length..].Split('?')[0].Trim();

            AddValidPhoneNumbers(result, new[] { phone });
        }
    }

    /// <summary>
    /// Finds addresses in semantic HTML and common address-related attributes.
    /// </summary>

    private static readonly Regex AddressIndicatorRegex = new(
        @"\b(road|rd\.?|street|st\.?|highway|distt\.?|district|sector|"
        + @"village|city|haryana|india|pin|postal code)\b|\b\d{6}\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static void ExtractAddresses(
        HtmlDocument document,
        ContactInformation result)
    {
        var addressNodes = document.DocumentNode.SelectNodes(
            "//address | " +
            "//*[@itemprop='address'] | " +
            "//*[contains(translate(@class,'ADDRESS','address'),'address')] | " +
            "//*[contains(translate(@id,'ADDRESS','address'),'address')] | " +
            "//p");

        if (addressNodes == null)
            return;

        foreach (var node in addressNodes)
        {
            var address = CleanText(node.InnerText);

            if (string.IsNullOrWhiteSpace(address))
                continue;

            // Paragraphs are considered addresses only when
            // they contain recognizable address indicators.
            bool isSemanticAddress =
                node.Name.Equals("address", StringComparison.OrdinalIgnoreCase) ||
                node.GetAttributeValue("itemprop", "")
                    .Equals("address", StringComparison.OrdinalIgnoreCase) ||
                node.GetAttributeValue("class", "")
                    .Contains("address", StringComparison.OrdinalIgnoreCase) ||
                node.GetAttributeValue("id", "")
                    .Contains("address", StringComparison.OrdinalIgnoreCase);

            bool hasAddressIndicators =
                AddressIndicatorRegex.IsMatch(address);

            if (isSemanticAddress || hasAddressIndicators)
            {
                AddUnique(result.Addresses, new[] { address });
            }
        }
    }


    /// <summary>
    /// Finds links pointing to recognised social media platforms.
    /// </summary>
    private static void ExtractSocialLinks(
        HtmlDocument document,
        ContactInformation result)
    {
        var links = document.DocumentNode.SelectNodes("//a[@href]");

        if (links == null)
            return;

        foreach (var link in links)
        {
            var href = link.GetAttributeValue("href", "").Trim();

            if (!Uri.TryCreate(href, UriKind.Absolute, out var uri))
                continue;

            if (uri.Scheme != Uri.UriSchemeHttp &&
                uri.Scheme != Uri.UriSchemeHttps)
                continue;

            var host = uri.Host.ToLowerInvariant();

            string? platform = host switch
            {
                "linkedin.com" or "www.linkedin.com" => "LinkedIn",
                "facebook.com" or "www.facebook.com" => "Facebook",
                "instagram.com" or "www.instagram.com" => "Instagram",
                "x.com" or "www.x.com" or
                "twitter.com" or "www.twitter.com" => "X",
                "youtube.com" or "www.youtube.com" => "YouTube",
                _ => null
            };

            if (platform == null)
                continue;

            if (!result.SocialLinks.ContainsKey(platform))
                result.SocialLinks[platform] = new List<string>();

            AddUnique(result.SocialLinks[platform], new[] { uri.AbsoluteUri });
        }
    }

    /// <summary>
    /// Validates and adds phone number candidates.
    /// </summary>
    private static void AddValidPhoneNumbers(
        ContactInformation result,
        IEnumerable<string> candidates)
    {
        foreach (var candidate in candidates)
        {
            var digitCount = candidate.Count(char.IsDigit);

            if (digitCount >= 7 && digitCount <= 15)
            {
                AddUnique(
                    result.PhoneNumbers,
                    new[] { CleanText(candidate) });
            }
        }
    }

    /// <summary>
    /// Normalises whitespace in extracted text.
    /// </summary>
    private static string CleanText(string value)
    {
        return Regex.Replace(
            HtmlEntity.DeEntitize(value ?? string.Empty),
            @"\s+",
            " ").Trim();
    }

    /// <summary>
    /// Adds values only when they are not already present.
    /// </summary>
    private static void AddUnique(
        List<string> destination,
        IEnumerable<string> values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value) &&
                !destination.Contains(
                    value,
                    StringComparer.OrdinalIgnoreCase))
            {
                destination.Add(value);
            }
        }
    }
}
