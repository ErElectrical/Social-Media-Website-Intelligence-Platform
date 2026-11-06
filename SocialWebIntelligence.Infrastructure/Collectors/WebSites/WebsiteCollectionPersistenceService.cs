
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SocialWebIntelligence.Core.Entities;
using SocialWebIntelligence.Core.DTOs;
using SocialWebIntelligence.Infrastructure.Data;

namespace SocialWebIntelligence.Infrastructure.Collectors.Websites;

/// <summary>
/// Saves extracted website contact information into MariaDB.
/// </summary>
public class WebsiteCollectionPersistenceService
{
    private readonly SocialWebIntelligenceDbContext _dbContext;

    public WebsiteCollectionPersistenceService(
        SocialWebIntelligenceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WebsiteCollection> SaveAsync(
        int monitoredSourceId,
        WebsiteContactCollectionResult result,
        CancellationToken cancellationToken = default)
    {
        if (monitoredSourceId <= 0)
            throw new ArgumentException(
                "Invalid monitored source ID.",
                nameof(monitoredSourceId));

        ArgumentNullException.ThrowIfNull(result);

        // Verify that the company is registered.
        var sourceExists = await _dbContext.MonitoredSources
            .AnyAsync(
                source => source.Id == monitoredSourceId,
                cancellationToken);

        if (!sourceExists)
        {
            throw new InvalidOperationException(
                $"Monitored source {monitoredSourceId} does not exist.");
        }

        // Convert extracted contact information into JSON.
        var extractedDataJson = JsonSerializer.Serialize(
            result.ContactInformation,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

        // Create the database entity.
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

        // Add the entity to EF Core's change tracker.
        await _dbContext.WebsiteCollections.AddAsync(
            collection,
            cancellationToken);

        // Execute INSERT in MariaDB.
        await _dbContext.SaveChangesAsync(cancellationToken);

        return collection;
    }
}
