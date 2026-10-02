using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SUI.GetAnIdentifier.SmokeTests;

/// <summary>
/// These tests are to be run post-deployment in order to ensure expected
/// Get-An-Identifier solution behaviour is exhibited across environments.
/// </summary>
[Trait("Category", "Smoke")]
public class GetAnIdentifierSmokeTests : IDisposable
{
    private readonly HttpClient _client;
    private string? _bearerToken;

    public GetAnIdentifierSmokeTests()
    {
        var baseUrl =
            Environment.GetEnvironmentVariable("SMOKE_TEST_BASE_URL")
            ?? throw new InvalidOperationException("SMOKE_TEST_BASE_URL is missing.");

        _client = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    private async Task<string> GetBearerTokenAsync()
    {
        if (!string.IsNullOrEmpty(_bearerToken))
            return _bearerToken;

        var tokenUrl =
            Environment.GetEnvironmentVariable("SMOKE_TEST_TOKEN_URL")
            ?? throw new InvalidOperationException("SMOKE_TEST_TOKEN_URL is missing.");

        var clientId =
            Environment.GetEnvironmentVariable("SMOKE_TEST_CLIENT_ID")
            ?? throw new InvalidOperationException("SMOKE_TEST_CLIENT_ID is missing.");

        var clientSecret =
            Environment.GetEnvironmentVariable("SMOKE_TEST_CLIENT_SECRET")
            ?? throw new InvalidOperationException("SMOKE_TEST_CLIENT_SECRET is missing.");

        // Scope is optional
        var scope = Environment.GetEnvironmentVariable("SMOKE_TEST_AUTH_SCOPE");

        using var authClient = new HttpClient();

        // Basic Auth Header
        var authString = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")
        );
        authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            authString
        );

        var formValues = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "client_credentials"),
            new("client_id", clientId),
            new("client_secret", clientSecret),
        };

        // Only attach scope if explicitly set for the target environment
        if (!string.IsNullOrWhiteSpace(scope))
        {
            formValues.Add(new("scope", scope));
        }

        using var requestContent = new FormUrlEncodedContent(formValues);
        var response = await authClient.PostAsync(tokenUrl, requestContent);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Token request failed ({response.StatusCode}). ClientID: '{clientId}'. URL: {tokenUrl}. Body: {errorBody}"
            );
        }

        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        _bearerToken =
            json?["access_token"]?.ToString()
            ?? throw new InvalidOperationException(
                "OIDC response did not contain an access_token."
            );

        return _bearerToken;
    }

    [Fact]
    public async Task HealthEndpoint_ShouldReturnOk()
    {
        using var response = await _client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAnIdentifier_WithoutBearerToken_ShouldReturnUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/get-an-identifier");
        request.Content = CreateSyntheticPayload();

        using var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAnIdentifier_WithValidAuth_ShouldSuccessfullyReachPds()
    {
        var token = await GetBearerTokenAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/get-an-identifier");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = CreateSyntheticPayload();

        using var response = await _client.SendAsync(request);

        var validOutcomes = new[] { HttpStatusCode.OK, HttpStatusCode.NotFound };
        Assert.True(
            validOutcomes.Contains(response.StatusCode),
            $"Expected OK or NotFound, but got {response.StatusCode}. This indicates NHS OAuth or PDS connectivity failed."
        );
    }

    private static JsonContent CreateSyntheticPayload()
    {
        var syntheticData = new
        {
            personSpecification = new
            {
                given = "Octavia",
                family = "Chislett",
                birthDate = "2022-03-17",
                gender = "female",
                addressPostalCode = "KT19 0ST",
            },
        };

        return JsonContent.Create(
            syntheticData,
            options: new JsonSerializerOptions(JsonSerializerDefaults.Web)
        );
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}
