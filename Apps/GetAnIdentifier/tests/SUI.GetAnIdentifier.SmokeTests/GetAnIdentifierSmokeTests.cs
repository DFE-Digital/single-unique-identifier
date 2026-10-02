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

        var clientIdsJson =
            Environment.GetEnvironmentVariable("SMOKE_TEST_CLIENT_IDS_JSON_MAP")
            ?? throw new InvalidOperationException("SMOKE_TEST_CLIENT_IDS_JSON_MAP is missing.");

        var clientSecretsJson =
            Environment.GetEnvironmentVariable("SMOKE_TEST_CLIENT_SECRETS_JSON_MAP")
            ?? throw new InvalidOperationException(
                "SMOKE_TEST_CLIENT_SECRETS_JSON_MAP is missing."
            );

        // Scope is optional - passed only if configured (e.g. FaUAPI in d02/d03)
        var scope = Environment.GetEnvironmentVariable("SMOKE_TEST_AUTH_SCOPE");

        var clientIds =
            JsonSerializer.Deserialize<Dictionary<string, string>>(clientIdsJson)
            ?? throw new InvalidOperationException("Failed to parse Client IDs map.");

        var clientSecrets =
            JsonSerializer.Deserialize<Dictionary<string, string>>(clientSecretsJson)
            ?? throw new InvalidOperationException("Failed to parse Client Secrets map.");

        var clientKey =
            clientIds.Keys.FirstOrDefault()
            ?? throw new InvalidOperationException("Client IDs map is empty.");

        var clientId = clientIds[clientKey];
        var clientSecret = clientSecrets[clientKey];

        using var authClient = new HttpClient();

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
        response.EnsureSuccessStatusCode();

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

        //TODO: tidy up after testing
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Token request failed with status {response.StatusCode}. Error Body: {errorBody}"
            );
        }

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
