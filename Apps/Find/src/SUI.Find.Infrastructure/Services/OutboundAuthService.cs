using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Domain.Models;
using SUI.Find.Infrastructure.Models;

namespace SUI.Find.Infrastructure.Services;

public class OutboundAuthService(
    ILogger<OutboundAuthService> logger,
    IHttpClientFactory httpClientFactory
) : IOutboundAuthService
{
    private static readonly ConcurrentDictionary<string, CachedToken> Cache = new();

    public async Task<Result<string>> GetAccessTokenAsync(
        ProviderDefinition providerDefinition,
        CancellationToken cancellationToken
    )
    {
        var auth = providerDefinition.Connection.Auth;
        if (auth is null)
        {
            logger.LogError(
                "No auth configuration for provider {Provider}",
                providerDefinition.ProviderName
            );
            return Result<string>.Fail("No auth configuration for provider.");
        }

        if (
            string.IsNullOrWhiteSpace(auth.TokenUrl)
            || string.IsNullOrWhiteSpace(auth.ClientId)
            || string.IsNullOrWhiteSpace(auth.ClientSecret)
        )
        {
            logger.LogError(
                "Invalid auth configuration for provider {Provider}",
                providerDefinition.ProviderName
            );
            return Result<string>.Fail("Invalid auth configuration for provider.");
        }

        var scopeString = auth.Scopes.Count == 0 ? "" : string.Join(" ", auth.Scopes);

        var cacheKey = $"{auth.TokenUrl}||{auth.ClientId}||{scopeString}";

        using var request = new HttpRequestMessage(HttpMethod.Post, auth.TokenUrl);
        logger.LogInformation(
            "Token request to {TokenUrl} for provider {Provider}",
            auth.TokenUrl,
            providerDefinition.ProviderName
        );
        request.Content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = auth.ClientId,
                ["client_secret"] = auth.ClientSecret,
                ["scope"] = scopeString,
            }
        );

        using var http = httpClientFactory.CreateClient("providers");
        using var res = await http.SendAsync(request, cancellationToken);
        if (!res.IsSuccessStatusCode)
        {
            logger.LogError(
                "Failed to obtain access token from provider {Provider}. Status code: {StatusCode}",
                providerDefinition.ProviderName,
                res.StatusCode
            );
            return Result<string>.Fail("Failed to obtain access token from provider.");
        }

        var json = await res.Content.ReadAsStringAsync(cancellationToken);
        var token = JsonSerializer.Deserialize<OAuthTokenResponse>(json, JsonSerializerOptions.Web);

        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            logger.LogError(
                "Invalid token response from provider {Provider}",
                providerDefinition.ProviderName
            );
            return Result<string>.Fail("Invalid token response from provider.");
        }

        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, token.ExpiresIn));
        Cache[cacheKey] = new CachedToken(token.AccessToken, expiresAt);

        return Result<string>.Ok(token.AccessToken);
    }
}
