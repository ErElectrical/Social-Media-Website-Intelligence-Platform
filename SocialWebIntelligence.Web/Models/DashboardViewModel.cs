
namespace SocialWebIntelligence.Web.ViewModels;

/// <summary>
/// Holds the database-backed information displayed on the main dashboard.
/// Keeping this separate from database entities prevents the dashboard
/// from depending directly on the database's internal structure.
/// </summary>
public class DashboardViewModel
{
    public int TotalCompanies { get; set; }

    public int ActiveCompanies { get; set; }

    public int TotalCollections { get; set; }

    public int TotalChanges { get; set; }

    public List<DailyCollectionActivity> CollectionActivity { get; set; } = new();

    public List<RecentWebsiteChange> RecentChanges { get; set; } = new();
}

/// <summary>
/// Represents the number of website collection snapshots saved on a particular day.
/// </summary>
public class DailyCollectionActivity
{
    public DateTime Date { get; set; }

    public int CollectionCount { get; set; }
}

/// <summary>
/// Combines a detected change with the company and page where it occurred.
/// </summary>
public class RecentWebsiteChange
{
    public string CompanyName { get; set; } = string.Empty;

    public string PageTitle { get; set; } = string.Empty;

    public string Field { get; set; } = string.Empty;

    public string ChangeType { get; set; } = string.Empty;

    public DateTime DetectedAt { get; set; }
}
