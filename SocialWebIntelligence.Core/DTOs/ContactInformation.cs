
using static System.Net.Mime.MediaTypeNames;

namespace SocialWebIntelligence.Core.DTOs;


/// Represents contact information extracted from a website.
/// This is a data-transfer model, not a database entity.
/// 
//Why DTO instead of Entity?
//This distinction is important:
//- Entity: Represents something persisted and managed in the database.
//- DTO: Represents data transferred between application components.

public class ContactInformation
{
    
    /// Email addresses found on the website.

    public List<string> Emails { get; set; } = new();

    /// Telephone numbers found on the website.
   
    public List<string> PhoneNumbers { get; set; } = new();

    
    /// Physical addresses found on the website.
    
    public List<string> Addresses { get; set; } = new();

    
    /// Social media links grouped by platform name.
    /// Example: LinkedIn, Facebook, X.
   
    public Dictionary<string, List<string>> SocialLinks { get; set; }
        = new(StringComparer.OrdinalIgnoreCase);
}
