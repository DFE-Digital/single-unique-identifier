using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Utilities;

public class TokenProvider : ITokenProvider
{
    private readonly HttpClient _httpClient;

    private readonly Dictionary<string, (string Token, DateTimeOffset Expiry)> _cache = new();

    public TokenProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetTokenAsync(string clientId, string clientSecret)
    {
        var cacheKey = $"{clientId}:{clientSecret}";

        if (_cache.TryGetValue(cacheKey, out var entry) && DateTimeOffset.UtcNow < entry.Expiry)
        {
            return entry.Token;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/token");

        var creds = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", creds);

        request.Content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["scope"] = "work-item.write",
            }
        );

        using var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Get Access Token Failed: {(int)response.StatusCode} ({response.StatusCode}) - {errorBody}"
            );
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        var token = JsonSerializer.Deserialize<AuthTokenResponse>(json, JsonSerializerOptions.Web)!;

        var expiry = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn - 30);

        _cache[cacheKey] = (token.AccessToken, expiry);

        return token.AccessToken;
    }
}
