using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Interfaces;
using SUI.Find.Domain.Models;

namespace SUI.Find.Infrastructure.Utility;

public class ProviderHttpClient(IHttpClientFactory httpClientFactory, ILogger<ProviderHttpClient> logger) : IProviderHttpClient
{
    public async Task<Result<string>> GetAsync(string url, string? bearerToken, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("providers");

        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        try
        {
            using var response = await client.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogInformation("Provider returned 404 Not Found for URL: {Url}", url);
                return Result<string>.Fail("No record found.");
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Provider returned unexpected status: {StatusCode} for URL: {Url}", response.StatusCode, url);
                return Result<string>.Fail($"Custodian returned unexpected status: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            return Result<string>.Ok(content);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP Request failed for URL: {Url}", url);
            return Result<string>.Fail($"Connectivity Error: {ex.Message}");
        }
    }
}