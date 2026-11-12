namespace SocialWebIntelligence.Core.DTOs;

/// <summary>
/// Stores common structured information extracted
/// from Products, Services, News and Careers pages.
/// </summary>
public class GeneralPageInformation
{
    public string? PageTitle { get; set; }

    public string? MetaDescription { get; set; }

    public string? MainHeading { get; set; }

    public List<string> Headings { get; set; } = new();

    public List<string> DescriptionParagraphs { get; set; } = new();

    public List<string> ListItems { get; set; } = new();
}
