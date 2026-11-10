using SocialWebIntelligence.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialWebIntelligence.Core.DTOs
{
    public enum WebsiteCollectionSaveStatus
    {
        New = 1,
        Changed = 2,
        Unchanged = 3
    }
    public class WebsiteCollectionSaveResult
    {

        //it contains the database record
        public WebsiteCollection Collection { get; set; } = null!;

        //it contains what happend during comparison
        public WebsiteCollectionSaveStatus Status { get; set; }

        public List<ContactInformationChange> Changes { get; set; } = new();
    }
}
