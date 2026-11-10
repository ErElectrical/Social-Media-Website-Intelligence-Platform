using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialWebIntelligence.Core.DTOs
{
    public class ContactInformationChange
    {

        public string Field { get; set; } = string.Empty;

        // Possible values: Added, Removed
        public string ChangeType { get; set; } = string.Empty;

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

    }
}
