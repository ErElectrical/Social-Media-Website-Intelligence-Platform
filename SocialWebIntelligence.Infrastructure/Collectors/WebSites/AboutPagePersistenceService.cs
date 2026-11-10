
using Microsoft.EntityFrameworkCore;
using SocialWebIntelligence.Core.DTOs;
using SocialWebIntelligence.Core.Entities;
using SocialWebIntelligence.Infrastructure.Data;
using System.Text.Json;

namespace SocialWebIntelligence.Infrastructure.Collectors.Websites;

/// <summary>
/// Persists About page snapshots and detects changes between collections.
/// </summary>
public class AboutPagePersistenceService
{
    private readonly SocialWebIntelligenceDbContext _dbContext;

    public AboutPagePersistenceService(SocialWebIntelligenceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WebsiteCollectionSaveResult> SaveAsync(
        int monitoredSourceId,
        AboutPageCollectionResult result,
        CancellationToken cancellationToken = default)
    {
        if (monitoredSourceId <= 0)
            throw new ArgumentException("Invalid monitored source ID.");

        ArgumentNullException.ThrowIfNull(result);

        if (string.IsNullOrWhiteSpace(result.PageUrl))
            throw new ArgumentException("Page URL is required.");

        if (string.IsNullOrWhiteSpace(result.ContentHash))
            throw new ArgumentException("Content hash is required.");

        ArgumentNullException.ThrowIfNull(result.AboutInformation);

        // Confirm that the source exists before saving its collection.
        var sourceExists = await _dbContext.MonitoredSources
            .AnyAsync(x => x.Id == monitoredSourceId, cancellationToken);

        if (!sourceExists)
            throw new InvalidOperationException("Monitored source not found.");

        // Compare only snapshots belonging to this exact About page.
        var previous = await _dbContext.WebsiteCollections
            .Where(x =>
                x.MonitoredSourceId == monitoredSourceId &&
                x.PageType == "About" &&
                x.PageUrl == result.PageUrl &&
                x.IsSuccessful)
            .OrderByDescending(x => x.CollectedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var changes = new List<ContactInformationChange>();

        WebsiteCollectionSaveStatus status;

        if (previous == null)
        {
            status = WebsiteCollectionSaveStatus.New;
        }
        else if (string.Equals(
            previous.ContentHash,
            result.ContentHash,
            StringComparison.OrdinalIgnoreCase))
        {
            // Do not create duplicate snapshots when extracted information is unchanged.
            return new WebsiteCollectionSaveResult
            {
                Collection = previous,
                Status = WebsiteCollectionSaveStatus.Unchanged
            };
        }
        else
        {
            status = WebsiteCollectionSaveStatus.Changed;

            var oldInformation =
                JsonSerializer.Deserialize<AboutInformation>(
                    previous.ExtractedDataJson ?? "{}",
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new AboutInformation();

            // Record field-level differences to make future change reports useful.
            CompareValue("PageTitle", oldInformation.PageTitle,
                result.AboutInformation.PageTitle, changes);

            CompareValue("MetaDescription", oldInformation.MetaDescription,
                result.AboutInformation.MetaDescription, changes);

            CompareValue("MainHeading", oldInformation.MainHeading,
                result.AboutInformation.MainHeading, changes);

            CompareList("Heading", oldInformation.Headings,
                result.AboutInformation.Headings, changes);

            CompareList("DescriptionParagraph",
                oldInformation.DescriptionParagraphs,
                result.AboutInformation.DescriptionParagraphs, changes);
        }

        var json = JsonSerializer.Serialize(
            result.AboutInformation,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

        var collection = new WebsiteCollection
        {
            MonitoredSourceId = monitoredSourceId,
            PageUrl = result.PageUrl,
            PageTitle = result.PageTitle,
            PageType = "About",
            ExtractedDataJson = json,
            ContentHash = result.ContentHash,
            HttpStatusCode = result.HttpStatusCode,
            IsSuccessful = true,
            CollectedAt = DateTime.UtcNow
        };

        _dbContext.WebsiteCollections.Add(collection);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new WebsiteCollectionSaveResult
        {
            Collection = collection,
            Status = status,
            Changes = changes
        };
    }

    private static void CompareValue(
        string field,
        string? oldValue,
        string? newValue,
        List<ContactInformationChange> changes)
    {
        if (string.Equals(oldValue?.Trim(), newValue?.Trim(),
            StringComparison.OrdinalIgnoreCase))
            return;

        changes.Add(new ContactInformationChange
        {
            Field = field,
            ChangeType = "Modified",
            OldValue = oldValue,
            NewValue = newValue
        });
    }

    private static void CompareList(
        string field,
        List<string> oldValues,
        List<string> newValues,
        List<ContactInformationChange> changes)
    {
        var oldSet = new HashSet<string>(
            oldValues.Where(x => !string.IsNullOrWhiteSpace(x)),
            StringComparer.OrdinalIgnoreCase);

        var newSet = new HashSet<string>(
            newValues.Where(x => !string.IsNullOrWhiteSpace(x)),
            StringComparer.OrdinalIgnoreCase);

        foreach (var removed in oldSet.Except(newSet))
        {
            changes.Add(new ContactInformationChange
            {
                Field = field,
                ChangeType = "Removed",
                OldValue = removed
            });
        }

        foreach (var added in newSet.Except(oldSet))
        {
            changes.Add(new ContactInformationChange
            {
                Field = field,
                ChangeType = "Added",
                NewValue = added
            });
        }
    }
}
