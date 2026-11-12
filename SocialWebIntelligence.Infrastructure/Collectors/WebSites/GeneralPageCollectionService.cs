using Microsoft.VisualBasic;
using SocialWebIntelligence.Core.DTOs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using static System.Net.WebRequestMethods;

namespace SocialWebIntelligence.Infrastructure.Collectors.Websites;

public class GeneralPageCollectionService
{
    private readonly HttpClient _httpClient;
    private readonly GeneralPageInformationExtractor _extractor;

    public GeneralPageCollectionService(
        HttpClient httpClient,
        GeneralPageInformationExtractor extractor)
    {
        _httpClient = httpClient;
        _extractor = extractor;
    }



    //How the service works

        //1. Validate URL — accepts only valid absolute HTTP or HTTPS URLs.
        //2. Download HTML — uses the injected HttpClient to retrieve the page.
        //3. Extract information — passes HTML to GeneralPageInformationExtractor.
        //4. Generate content hash — hashes the serialized structured information using SHA-256.
        //5. Return result — provides the URL, title, HTTP status, hash and extracted information.

    public async Task<GeneralPageCollectionResult> CollectAsync(
        string pageUrl,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "A valid HTTP or HTTPS page URL is required.",
                nameof(pageUrl));
        }

        using var response = await _httpClient.GetAsync(
            uri,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(
            cancellationToken);

        var information = _extractor.Extract(html);

        var serializedInformation = JsonSerializer.Serialize(
            information,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

        var contentHash = Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(serializedInformation)));

        return new GeneralPageCollectionResult
        {
            PageUrl = response.RequestMessage?.RequestUri?.AbsoluteUri
                      ?? uri.AbsoluteUri,

            PageTitle = information.PageTitle,

            HttpStatusCode = (int)response.StatusCode,

            ContentHash = contentHash,

            Information = information
        };
    }
}