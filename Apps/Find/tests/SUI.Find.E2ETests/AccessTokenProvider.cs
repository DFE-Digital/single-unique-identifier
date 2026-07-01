using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Polly;

namespace SUI.Find.E2ETests;

public class AccessTokenProvider(FunctionTestFixture testFixture)
{
    public async Task<string?> GetAuthTokenAsync(
        string clientId,
        string clientSecret,
        string?[]? scopes,
        ITestOutputHelper testOutputHelper
    )
    {
        var originalClientId = clientId;

        clientId =
            testFixture.Configuration[$"E2E:AuthClientCredentials:{originalClientId}:NewClientId"]
            ?? clientId;

        clientSecret =
            testFixture.Configuration[
                $"E2E:AuthClientCredentials:{originalClientId}:NewClientSecret"
            ]
            ?? clientSecret;

        return await GetAuthTokenWithRetryAsync(
            scopes,
            clientId,
            clientSecret,
            testOutputHelper,
            isClientIdSensitive: clientId != originalClientId
        );
    }

    private async Task<string> GetAuthTokenWithRetryAsync(
        string?[]? scopes,
        string clientId,
        string clientSecret,
        ITestOutputHelper testOutputHelper,
        bool isClientIdSensitive
    )
    {
        var authString = $"{clientId}:{clientSecret}";
        var base64Auth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(authString));
        var clientCredentials = new AuthenticationHeaderValue("Basic", base64Auth);

        const int retryCount = 12;
        var waitInterval = TimeSpan.FromSeconds(15);

        var retryPolicy = Policy
            .Handle<Exception>(ex =>
            {
                bool retry;
                if (ex is HttpRequestException httpEx)
                {
                    testOutputHelper.WriteLine(
                        $"Warning: exception while attempting to get auth token: {nameof(HttpRequestException)}:{httpEx.StatusCode} - {httpEx.Message}"
                    );

                    retry = httpEx.StatusCode switch
                    {
                        HttpStatusCode.BadRequest
                        or HttpStatusCode.Unauthorized
                        or HttpStatusCode.Forbidden
                        or HttpStatusCode.InternalServerError => false,
                        _ => true, // default to retrying for all other http exceptions
                    };

                    return retry;
                }

                testOutputHelper.WriteLine(
                    $"Warning: exception while attempting to get auth token: {ex.Message}"
                );

                retry = false;
                return retry;
            })
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt =>
                {
                    testOutputHelper.WriteLine(
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
                { "scope", string.Join(" ", scopes ?? []) },
            };

            using var content = new FormUrlEncodedContent(formData);
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                testFixture.Config.AccessTokenUrl
            );

            request.Content = content;
            request.Headers.Authorization = clientCredentials;

            testOutputHelper.WriteLine(
                $"Requesting access token from: {request.RequestUri} for client ID: {FunctionTestFixture.MaskValue(clientId, isClientIdSensitive)}"
            );

            var response = await testFixture.Client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"Auth Failed: {response.StatusCode} - {error}",
                    statusCode: response.StatusCode,
                    inner: null
                );
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            var accessToken = result.GetProperty("access_token").GetString();
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new InvalidOperationException("No access_token in response");
            }

            testOutputHelper.WriteLine("Access token received OK");
            return accessToken;
        });
    }
}
