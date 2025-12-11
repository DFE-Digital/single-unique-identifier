using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Interfaces;
using SUI.Find.Domain.Models;

namespace SUI.Find.Infrastructure.Utility;

public class ProviderHttpClient(
    IHttpClientFactory httpClientFactory,
    ILogger<ProviderHttpClient> logger
) : IProviderHttpClient
{
    public async Task<Result<string>> GetAsync(
        string url,
        string? bearerToken,
        CancellationToken ct
    )
    {
        var client = httpClientFactory.CreateClient(ApplicationConstants.Providers.LoggingName);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        return await SendRequestAsync(client, request, ct);
    }

    public async Task<Result<string>> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(ApplicationConstants.Providers.LoggingName);
        return await SendRequestAsync(client, request, ct);
    }

    private async Task<Result<string>> SendRequestAsync(
        HttpClient client,
        HttpRequestMessage request,
        CancellationToken ct
    )
    {
        try
        {
            using var response = await client.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogInformation("Http Provider returned: {StatusCode}", response.StatusCode);
                return Result<string>.Fail("NotFound");
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Http Provider returned: {StatusCode}", response.StatusCode);
                return Result<string>.Fail($"StatusCode: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            return Result<string>.Ok(content);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP Request failed");
            return Result<string>.Fail($"Connection Error: {ex.Message}");
        }
    }
}
