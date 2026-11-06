
using SocialWebIntelligence.Infrastructure.Collectors.Websites;
using SocialWebIntelligence.Infrastructure.Collectors.WebSites;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace SocialWebIntelligence.Tests;

public class WebsiteLiveCollectionTests
{
    private readonly ITestOutputHelper _output;

    public WebsiteLiveCollectionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task CollectAsync_ShouldExtractContactInformationFromLiveWebsite()
    {
        const string websiteUrl = "https://jasch.net.in/";

        using var discoveryHttpClient = new HttpClient();
        using var collectionHttpClient = new HttpClient();

        discoveryHttpClient.Timeout = TimeSpan.FromSeconds(30);
        collectionHttpClient.Timeout = TimeSpan.FromSeconds(30);

        discoveryHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "SocialWebIntelligence/1.0");

        collectionHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "SocialWebIntelligence/1.0");

        var discoveryService =
            new WebsitePageDiscoveryService(discoveryHttpClient);

        var extractor = new ContactInformationExtractor();

        var collectionService = new WebsiteContactCollectionService(
            collectionHttpClient,
            discoveryService,
            extractor);

        _output.WriteLine($"Testing website: {websiteUrl}");

        // Execute the actual collection process.
        var result = await collectionService.CollectAsync(websiteUrl);

        if (result == null)
        {
            _output.WriteLine(
                "No Contact Us page was discovered on the homepage.");

            return;
        }

        // Display extracted information as formatted JSON.
        var json = JsonSerializer.Serialize(
            result,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        _output.WriteLine("Extracted contact information:");
        _output.WriteLine(json);
    }
}
