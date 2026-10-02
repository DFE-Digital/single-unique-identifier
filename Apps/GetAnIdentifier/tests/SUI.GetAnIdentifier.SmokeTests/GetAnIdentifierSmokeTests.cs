using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using SUI.GetAnIdentifier.Application.Models;
using Xunit.Abstractions;

namespace SUI.GetAnIdentifier.SmokeTests;

/// <summary>
/// These tests are to be run post-deployment in order to ensure expected
/// Get-An-Identifier solution behaviour is exhibited across environments.
/// </summary>
[Trait("Category", "Smoke")]
public class GetAnIdentifierSmokeTests : IDisposable
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _client;
    private string? _bearerToken;

    public GetAnIdentifierSmokeTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
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

        // Scope is optional
        var scope = Environment.GetEnvironmentVariable("SMOKE_TEST_AUTH_SCOPE");

        var clientIds =
            JsonSerializer.Deserialize<Dictionary<string, string>>(clientIdsJson)
            ?? throw new InvalidOperationException("Failed to parse Client IDs map.");

        var clientSecrets =
            JsonSerializer.Deserialize<Dictionary<string, string>>(clientSecretsJson)
            ?? throw new InvalidOperationException("Failed to parse Client Secrets map.");

        // Grab the first valid client ID key from the dictionary
        var clientKey =
            clientIds.Keys.FirstOrDefault()
            ?? throw new InvalidOperationException("Client IDs map is empty.");

        var clientId = clientIds[clientKey];
        var clientSecret = clientSecrets[clientKey];

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

        //response.EnsureSuccessStatusCode();
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

        // TEMPORARY DEBUG: Decode the JWT payload to see the claims
        var tokenParts = token.Split('.');
        if (tokenParts.Length >= 2)
        {
            var payload = tokenParts[1];
            payload = payload.Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var decodedPayload = System.Text.Encoding.UTF8.GetString(
                Convert.FromBase64String(payload)
            );

            // If the request fails, print this decoded token to read the claims
            _testOutputHelper.WriteLine("DEBUG TOKEN PAYLOAD: " + decodedPayload);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/get-an-identifier");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = CreateSyntheticPayload();

        using var response = await _client.SendAsync(request);

        var errorBody = !response.IsSuccessStatusCode
            ? await response.Content.ReadAsStringAsync()
            : string.Empty;

        var validOutcomes = new[] { HttpStatusCode.OK, HttpStatusCode.NotFound };

        // Include the decoded claims in the failure message
        var debugClaims =
            tokenParts.Length >= 2
                ? System.Text.Encoding.UTF8.GetString(
                    Convert.FromBase64String(
                        tokenParts[1]
                            .Replace('-', '+')
                            .Replace('_', '/')
                            .PadRight(
                                tokenParts[1].Length + (4 - tokenParts[1].Length % 4) % 4,
                                '='
                            )
                    )
                )
                : "N/A";

        Assert.True(
            validOutcomes.Contains(response.StatusCode),
            $"Expected OK or NotFound, but got {response.StatusCode}. API Error: {errorBody}. Token Payload: {debugClaims}"
        );
    }

    private static StringContent CreateSyntheticPayload()
    {
        var json = """
            {
              "PersonSpecification": {
                "Given": "Octavia",
                "Family": "Chislett",
                "BirthDate": "2022-03-17",
                "Gender": "female",
                "AddressPostalCode": "KT19 0ST"
              }
            }
            """;

        var content = new StringContent(json, System.Text.Encoding.UTF8);

        // Strip charset to ensure strict "application/json" matching
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        return content;
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}
