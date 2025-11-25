using Models;
using SUI.Find.Functions.Interfaces;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Services;

public sealed class OutboundAuthTokenService(IHttpClientFactory httpClientFactory)
    : IOutboundAuthTokenService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    private static readonly ConcurrentDictionary<string, CachedToken> Cache = new();

    public async Task<string?> GetAccessTokenAsync(AuthDefinition auth, CancellationToken ct)
    {
        if (auth is null) return null;

        if (!string.Equals(auth.Type, "oauth2_client_credentials", StringComparison.OrdinalIgnoreCase))
            return null;

        if (string.IsNullOrWhiteSpace(auth.TokenUrl) ||
            string.IsNullOrWhiteSpace(auth.ClientId) ||
            string.IsNullOrWhiteSpace(auth.ClientSecret))
            return null;

        var scopeString = auth.Scopes is null || auth.Scopes.Count == 0
            ? ""
            : string.Join(" ", auth.Scopes);

        var cacheKey = $"{auth.TokenUrl}||{auth.ClientId}||{scopeString}";

        if (Cache.TryGetValue(cacheKey, out var cached) &&
            cached.ExpiresAtUtc > DateTimeOffset.UtcNow.AddSeconds(30))
        {
            return cached.AccessToken;
        }

        using var http = _httpClientFactory.CreateClient("providers");

        using var req = new HttpRequestMessage(HttpMethod.Post, auth.TokenUrl);
        req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = auth.ClientId,
            ["client_secret"] = auth.ClientSecret,
            ["scope"] = scopeString
        });

        using var res = await http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await res.Content.ReadAsStringAsync(ct);
        var token = JsonSerializer.Deserialize<OAuthTokenResponse>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (token is null || string.IsNullOrWhiteSpace(token.Access_Token))
            return null;

        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, token.Expires_In));
        Cache[cacheKey] = new CachedToken(token.Access_Token, expiresAt);

        return token.Access_Token;
    }

    private sealed record CachedToken(string AccessToken, DateTimeOffset ExpiresAtUtc);
}