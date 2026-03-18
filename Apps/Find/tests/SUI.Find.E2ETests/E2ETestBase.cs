using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Polly;
using Xunit.Abstractions;

namespace SUI.Find.E2ETests;

public class E2ETestBase
{
    protected readonly FunctionTestFixture Fixture;

    protected readonly ITestOutputHelper TestOutputHelper;

    protected E2ETestBase(FunctionTestFixture fixture, ITestOutputHelper testOutputHelper)
    {
        Fixture = fixture;
        TestOutputHelper = testOutputHelper;

        TestOutputHelper.WriteLine(Fixture.Config.ToString());
    }

    protected async Task<string?> GetAuthTokenAsync(
        string clientId,
        string clientSecret,
        string[] scopes
    )
    {
        var authString = $"{clientId}:{clientSecret}";
        var base64Auth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(authString));
        var clientCredentials = new AuthenticationHeaderValue("Basic", base64Auth);

        return await GetAuthTokenWithRetryAsync(scopes, clientCredentials);
    }

    private async Task<string> GetAuthTokenWithRetryAsync(
        string[] scopes,
        AuthenticationHeaderValue clientCredentials
    )
    {
        const int retryCount = 3;
        var waitInterval = TimeSpan.FromSeconds(10);

        var retryPolicy = Policy
            .Handle<Exception>(ex =>
            {
                TestOutputHelper.WriteLine(
                    $"Warning: exception while attempting to get auth token: {ex.Message}"
                );

                const bool retry = true;
                return retry;
            })
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt =>
                {
                    TestOutputHelper.WriteLine(
                        $"Failed to get auth token, waiting for {waitInterval.Seconds} seconds, then retrying, retry {retryAttempt} / {retryCount}..."
                    );
                    return waitInterval;
                }
            );

        return await retryPolicy.ExecuteAsync(async () =>
        {
            var formData = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "scope", string.Join(" ", scopes) },
            };

            using var content = new FormUrlEncodedContent(formData);
            using var request = new HttpRequestMessage(HttpMethod.Post, "v1/auth/token");

            request.Content = content;
            request.Headers.Authorization = clientCredentials;

            TestOutputHelper.WriteLine(
                "Requesting access token from: {0}{1}",
                Fixture.Client.BaseAddress,
                request.RequestUri
            );

            var response = await Fixture.Client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Auth Failed: {response.StatusCode} - {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            var accessToken = result.GetProperty("access_token").GetString();
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new InvalidOperationException("No access_token in response");
            }

            TestOutputHelper.WriteLine("Access token received OK");
            return accessToken;
        });
    }
}
