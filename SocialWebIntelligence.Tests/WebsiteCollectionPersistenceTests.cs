using Microsoft.EntityFrameworkCore;
using SocialWebIntelligence.Core.DTOs;
using SocialWebIntelligence.Core.Entities;
using SocialWebIntelligence.Infrastructure.Data;
using SocialWebIntelligence.Infrastructure.Collectors.Websites;

namespace SocialWebIntelligence.Tests;

public class WebsiteCollectionPersistenceTests
{
    private static async Task<SocialWebIntelligenceDbContext>
        CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<SocialWebIntelligenceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new SocialWebIntelligenceDbContext(options);

        await context.MonitoredSources.AddAsync(new MonitoredSource
        {
            Id = 1,
            CompanyName = "Test Company",
            Platform = SourcePlatform.Website,
            Url = "https://test.com",
            IsActive = true
        });

        await context.SaveChangesAsync();

        return context;
    }

    private static WebsiteContactCollectionResult CreateResult(
        ContactInformation information,
        string hash)
    {
        return new WebsiteContactCollectionResult
        {
            PageUrl = "https://test.com/contact",
            PageTitle = "Contact Us",
            HttpStatusCode = 200,
            ContentHash = hash,
            ContactInformation = information
        };
    }

    [Fact]
    public async Task FirstCollection_ShouldReportAllValuesAsAdded()
    {
        await using var context = await CreateContextAsync();
        var service = new WebsiteCollectionPersistenceService(context);

        var information = new ContactInformation
        {
            Emails = new() { "info@test.com" },
            PhoneNumbers = new() { "9876543210" },
            Addresses = new() { "Delhi, India" }
        };

        var result = await service.SaveAsync(
            1, CreateResult(information, "hash-1"));

        Assert.Equal(WebsiteCollectionSaveStatus.New, result.Status);

        Assert.Contains(result.Changes, x =>
            x.Field == "Email" &&
            x.ChangeType == "Added" &&
            x.NewValue == "info@test.com");

        Assert.Contains(result.Changes, x =>
            x.Field == "PhoneNumber" &&
            x.ChangeType == "Added" &&
            x.NewValue == "9876543210");

        Assert.Contains(result.Changes, x =>
            x.Field == "Address" &&
            x.ChangeType == "Added" &&
            x.NewValue == "Delhi, India");
    }

    [Fact]
    public async Task ChangedEmail_ShouldReportRemovedAndAddedValues()
    {
        await using var context = await CreateContextAsync();
        var service = new WebsiteCollectionPersistenceService(context);

        await service.SaveAsync(1, CreateResult(
            new ContactInformation
            {
                Emails = new() { "info@test.com" }
            }, "hash-1"));

        var result = await service.SaveAsync(1, CreateResult(
            new ContactInformation
            {
                Emails = new() { "sales@test.com" }
            }, "hash-2"));

        Assert.Equal(WebsiteCollectionSaveStatus.Changed, result.Status);

        Assert.Contains(result.Changes, x =>
            x.Field == "Email" &&
            x.ChangeType == "Removed" &&
            x.OldValue == "info@test.com");

        Assert.Contains(result.Changes, x =>
            x.Field == "Email" &&
            x.ChangeType == "Added" &&
            x.NewValue == "sales@test.com");
    }

    [Fact]
    public async Task AddedPhoneNumber_ShouldReportAdded()
    {
        await using var context = await CreateContextAsync();
        var service = new WebsiteCollectionPersistenceService(context);

        await service.SaveAsync(1, CreateResult(
            new ContactInformation(), "hash-1"));

        var result = await service.SaveAsync(1, CreateResult(
            new ContactInformation
            {
                PhoneNumbers = new() { "9876543210" }
            }, "hash-2"));

        Assert.Contains(result.Changes, x =>
            x.Field == "PhoneNumber" &&
            x.ChangeType == "Added" &&
            x.NewValue == "9876543210");
    }

    [Fact]
    public async Task RemovedAddress_ShouldReportRemoved()
    {
        await using var context = await CreateContextAsync();
        var service = new WebsiteCollectionPersistenceService(context);

        await service.SaveAsync(1, CreateResult(
            new ContactInformation
            {
                Addresses = new() { "Delhi, India" }
            }, "hash-1"));

        var result = await service.SaveAsync(1, CreateResult(
            new ContactInformation(), "hash-2"));

        Assert.Contains(result.Changes, x =>
            x.Field == "Address" &&
            x.ChangeType == "Removed" &&
            x.OldValue == "Delhi, India");
    }

    [Fact]
    public async Task ChangedSocialLink_ShouldReportRemovedAndAdded()
    {
        await using var context = await CreateContextAsync();
        var service = new WebsiteCollectionPersistenceService(context);

        await service.SaveAsync(1, CreateResult(
            new ContactInformation
            {
                SocialLinks = new()
                {
                    ["Facebook"] = new() { "https://facebook.com/old" }
                }
            }, "hash-1"));

        var result = await service.SaveAsync(1, CreateResult(
            new ContactInformation
            {
                SocialLinks = new()
                {
                    ["Facebook"] = new() { "https://facebook.com/new" }
                }
            }, "hash-2"));

        Assert.Contains(result.Changes, x =>
            x.Field == "SocialLink.Facebook" &&
            x.ChangeType == "Removed" &&
            x.OldValue == "https://facebook.com/old");

        Assert.Contains(result.Changes, x =>
            x.Field == "SocialLink.Facebook" &&
            x.ChangeType == "Added" &&
            x.NewValue == "https://facebook.com/new");
    }

    [Fact]
    public async Task SameHash_ShouldReturnUnchangedWithoutInsertingSnapshot()
    {
        await using var context = await CreateContextAsync();
        var service = new WebsiteCollectionPersistenceService(context);

        var information = new ContactInformation
        {
            Emails = new() { "info@test.com" }
        };

        await service.SaveAsync(1, CreateResult(information, "hash-1"));

        var result = await service.SaveAsync(
            1, CreateResult(information, "hash-1"));

        Assert.Equal(WebsiteCollectionSaveStatus.Unchanged, result.Status);
        Assert.Empty(result.Changes);

        Assert.Equal(1, await context.WebsiteCollections.CountAsync());
    }

    [Fact]
    public async Task MultipleChanges_ShouldReportEveryDifference()
    {
        await using var context = await CreateContextAsync();
        var service = new WebsiteCollectionPersistenceService(context);

        await service.SaveAsync(1, CreateResult(
            new ContactInformation
            {
                Emails = new() { "old@test.com" },
                PhoneNumbers = new() { "1111111111" }
            }, "hash-1"));

        var result = await service.SaveAsync(1, CreateResult(
            new ContactInformation
            {
                Emails = new() { "new@test.com" },
                PhoneNumbers = new() { "2222222222" },
                Addresses = new() { "Mumbai, India" }
            }, "hash-2"));

        Assert.Equal(5, result.Changes.Count);
    }
}