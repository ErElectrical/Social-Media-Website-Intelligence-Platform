
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialWebIntelligence.Infrastructure.Data;
using SocialWebIntelligence.Web.ViewModels;

namespace SocialWebIntelligence.Web.Controllers;

public class DashboardController : Controller
{
    private readonly SocialWebIntelligenceDbContext _context;

    public DashboardController(SocialWebIntelligenceDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        // Capture the current UTC date once so all dashboard calculations
        // use the same reporting window.
        var today = DateTime.UtcNow.Date;
        var activityStartDate = today.AddDays(-6);

        var model = new DashboardViewModel
        {
            // Count all registered companies, including inactive sources.
            TotalCompanies = await _context.MonitoredSources
                .CountAsync(cancellationToken),

            // Only active sources are included in this KPI.
            ActiveCompanies = await _context.MonitoredSources
                .CountAsync(x => x.IsActive, cancellationToken),

            // These are saved collection snapshots, not unique discovered URLs.
            TotalCollections = await _context.WebsiteCollections
                .CountAsync(cancellationToken),

            // Each record represents a detected field-level change.
            TotalChanges = await _context.WebsiteCollectionChanges
                .CountAsync(cancellationToken)
        };

        // Load collection timestamps for the last seven calendar days.
        // Grouping in memory avoids relying on provider-specific date
        // translation when using MariaDB through EF Core.
        var collectionDates = await _context.WebsiteCollections
            .Where(x => x.CollectedAt >= activityStartDate &&
                        x.CollectedAt < today.AddDays(1))
            .Select(x => x.CollectedAt)
            .ToListAsync(cancellationToken);

        // Include days with zero collections so the chart always has
        // seven consecutive dates.
        model.CollectionActivity = Enumerable
            .Range(0, 7)
            .Select(offset =>
            {
                var date = activityStartDate.AddDays(offset);

                return new DailyCollectionActivity
                {
                    Date = date,
                    CollectionCount = collectionDates.Count(
                        collectedAt => collectedAt.Date == date)
                };
            })
            .ToList();

        // Join changes to their collection snapshots and monitored sources
        // to show which company and page experienced each change.
        model.RecentChanges = await (
            from change in _context.WebsiteCollectionChanges
            join collection in _context.WebsiteCollections
                on change.WebsiteCollectionId equals collection.Id
            join source in _context.MonitoredSources
                on collection.MonitoredSourceId equals source.Id
            orderby change.DetectedAt descending
            select new RecentWebsiteChange
            {
                CompanyName = source.CompanyName,
                PageTitle = collection.PageTitle ?? collection.PageUrl,
                Field = change.Field,
                ChangeType = change.ChangeType,
                DetectedAt = change.DetectedAt
            })
            .Take(10)
            .ToListAsync(cancellationToken);

        return View(model);
    }
}
