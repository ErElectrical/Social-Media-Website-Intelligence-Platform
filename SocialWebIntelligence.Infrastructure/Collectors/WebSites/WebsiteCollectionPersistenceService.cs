
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SocialWebIntelligence.Core.Entities;
using SocialWebIntelligence.Core.DTOs;
using SocialWebIntelligence.Infrastructure.Data;

namespace SocialWebIntelligence.Infrastructure.Collectors.Websites;

/// <summary>
/// Saves extracted website contact information,
/// detects changes and preserves historical snapshots.
/// </summary>
public class WebsiteCollectionPersistenceService
{
    private readonly SocialWebIntelligenceDbContext _dbContext;

    public WebsiteCollectionPersistenceService(
        SocialWebIntelligenceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WebsiteCollectionSaveResult> SaveAsync(
        int monitoredSourceId,
        WebsiteContactCollectionResult result,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Validate input.
        if (monitoredSourceId <= 0)
        {
            throw new ArgumentException(
                "Invalid monitored source ID.",
                nameof(monitoredSourceId));
        }

        ArgumentNullException.ThrowIfNull(result);

        if (string.IsNullOrWhiteSpace(result.PageUrl))
        {
            throw new ArgumentException(
                "Page URL cannot be empty.",
                nameof(result));
        }

        if (string.IsNullOrWhiteSpace(result.ContentHash))
        {
            throw new ArgumentException(
                "Content hash cannot be empty.",
                nameof(result));
        }

        ArgumentNullException.ThrowIfNull(result.ContactInformation);

        // Step 2: Verify that the company exists.
        var sourceExists = await _dbContext.MonitoredSources
            .AnyAsync(
                source => source.Id == monitoredSourceId,
                cancellationToken);

        if (!sourceExists)
        {
            throw new InvalidOperationException(
                $"Monitored source {monitoredSourceId} does not exist.");
        }

        // Step 3: Find the latest successful Contact collection.
        var previousCollection = await _dbContext.WebsiteCollections
            .Where(collection =>
                collection.MonitoredSourceId == monitoredSourceId &&
                collection.PageType == "Contact" &&
                collection.IsSuccessful)
            .OrderByDescending(collection => collection.CollectedAt)
            .ThenByDescending(collection => collection.Id)
            .FirstOrDefaultAsync(cancellationToken);

        ContactInformation previousInformation;

        WebsiteCollectionSaveStatus status;

        // Step 4: Determine whether this is a new or changed collection.
        if (previousCollection == null)
        {
            previousInformation = new ContactInformation();

            status = WebsiteCollectionSaveStatus.New;
        }
        else if (string.Equals(
                     previousCollection.ContentHash,
                     result.ContentHash,
                     StringComparison.OrdinalIgnoreCase))
        {
            // Nothing changed. Do not insert another snapshot.
            return new WebsiteCollectionSaveResult
            {
                Collection = previousCollection,
                Status = WebsiteCollectionSaveStatus.Unchanged,
                Changes = new List<ContactInformationChange>()
            };
        }
        else
        {
            // Recover the previous extracted information from JSON.
            previousInformation =
                JsonSerializer.Deserialize<ContactInformation>(
                    previousCollection.ExtractedDataJson ?? "{}")
                ?? new ContactInformation();

            status = WebsiteCollectionSaveStatus.Changed;
        }

        // Step 5: Compare old and new contact information.
        var changes = CompareContactInformation(
            previousInformation,
            result.ContactInformation);

        // Step 6: Serialize the new contact information.
        var extractedDataJson = JsonSerializer.Serialize(
            result.ContactInformation,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

        // Step 7: Create a new historical snapshot.
        var collection = new WebsiteCollection
        {
            MonitoredSourceId = monitoredSourceId,
            PageUrl = result.PageUrl,
            PageTitle = result.PageTitle,
            PageType = "Contact",
            ExtractedDataJson = extractedDataJson,
            ContentHash = result.ContentHash,
            HttpStatusCode = result.HttpStatusCode,
            IsSuccessful = true,
            ErrorMessage = null,
            CollectedAt = DateTime.UtcNow
        };

        await _dbContext.WebsiteCollections.AddAsync(
            collection,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Step 8: Return the collection and detected differences.
        return new WebsiteCollectionSaveResult
        {
            Collection = collection,
            Status = status,
            Changes = changes
        };
    }

    /// <summary>
    /// Compares previous and current contact information.
    /// </summary>
    private static List<ContactInformationChange> CompareContactInformation(
        ContactInformation previous,
        ContactInformation current)
    {
        var changes = new List<ContactInformationChange>();

        CompareList(
            "Email",
            previous.Emails,
            current.Emails,
            changes);

        CompareList(
            "PhoneNumber",
            previous.PhoneNumbers,
            current.PhoneNumbers,
            changes);

        CompareList(
            "Address",
            previous.Addresses,
            current.Addresses,
            changes);

        // Compare social links platform by platform.
        var previousSocialLinks = previous.SocialLinks
            ?? new Dictionary<string, List<string>>(
                StringComparer.OrdinalIgnoreCase);

        var currentSocialLinks = current.SocialLinks
            ?? new Dictionary<string, List<string>>(
                StringComparer.OrdinalIgnoreCase);

        var platforms = previousSocialLinks.Keys
            .Concat(currentSocialLinks.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var platform in platforms)
        {
            previousSocialLinks.TryGetValue(
                platform, out var oldLinks);

            currentSocialLinks.TryGetValue(
                platform, out var newLinks);

            CompareList(
                $"SocialLink.{platform}",
                oldLinks,
                newLinks,
                changes);
        }

        return changes;
    }

    /// <summary>
    /// Finds added and removed values in a collection.
    /// </summary>
    private static void CompareList(
        string field,
        IEnumerable<string>? previousValues,
        IEnumerable<string>? currentValues,
        List<ContactInformationChange> changes)
    {
        var oldValues = NormalizeValues(previousValues);
        var newValues = NormalizeValues(currentValues);

        var oldSet = new HashSet<string>(
            oldValues,
            StringComparer.OrdinalIgnoreCase);

        var newSet = new HashSet<string>(
            newValues,
            StringComparer.OrdinalIgnoreCase);

        // Values that existed before but no longer exist.
        foreach (var oldValue in oldValues.Where(x => !newSet.Contains(x)))
        {
            changes.Add(new ContactInformationChange
            {
                Field = field,
                ChangeType = "Removed",
                OldValue = oldValue,
                NewValue = null
            });
        }

        // Values that exist now but did not exist before.
        foreach (var newValue in newValues.Where(x => !oldSet.Contains(x)))
        {
            changes.Add(new ContactInformationChange
            {
                Field = field,
                ChangeType = "Added",
                OldValue = null,
                NewValue = newValue
            });
        }
    }

    /// <summary>
    /// Normalizes values before comparing them.
    /// </summary>
    private static List<string> NormalizeValues(
        IEnumerable<string>? values)
    {
        return (values ?? Enumerable.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => Regex.Replace(x.Trim(), @"\s+", " "))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
