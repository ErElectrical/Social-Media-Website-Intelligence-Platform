namespace SocialWebIntelligence.Core.DTOs;

/// <summary>
/// Holds structured information extracted from a company's About page.
/// This DTO contains extracted content, not HTTP or database metadata.
/// </summary>
public class AboutInformation
{
    // The title displayed in the browser tab.
    public string? PageTitle { get; set; }

    // The SEO description provided in the HTML meta description tag.
    public string? MetaDescription { get; set; }

    // The primary heading, usually the company's main introduction.
    public string? MainHeading { get; set; }

    // Additional headings such as History, Vision, Mission and Values.
    public List<string> Headings { get; set; } = new();

    // Descriptive paragraphs containing information about the company.
    public List<string> DescriptionParagraphs { get; set; } = new();
}