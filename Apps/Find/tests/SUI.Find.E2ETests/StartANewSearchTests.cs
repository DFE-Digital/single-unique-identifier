using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Polly;
using SUI.Find.FindApi.Models;

namespace SUI.Find.E2ETests;

public class StartANewSearchTests(FunctionTestFixture fixture) : IClassFixture<FunctionTestFixture>
{
    public const string TestClientId = "LOCAL-AUTHORITY-01";
    public const string TestClientSecret = "SUIProject";
    public static readonly string[] TetScopes = ["find-record.write", "find-record.read"];

    [Fact]
    public async Task Should_PersistSearchData_When_OrchestrationCompletes()
    {
        var authToken = await GetAuthTokenAsync();
        var body = new StartSearchRequest("Cy13hyZL-4LSIwVy50p-Hg");
        var stringContent = new StringContent(JsonSerializer.Serialize(body));
        fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            authToken
        );
        var result = await fixture.Client.PostAsync("/api/v1/searches", stringContent);

        // We then want to assert the returned body and status code
        Assert.Equal(HttpStatusCode.Accepted, result.StatusCode);
        var content = await result.Content.ReadAsStringAsync();
        var typedContent = JsonSerializer.Deserialize<SearchJob>(content);
        Assert.False(string.IsNullOrEmpty(typedContent!.JobId));

        // Step 2, check the status
        await WaitForCompletionAsync(typedContent.JobId);
    }

    private async Task<string?> GetAuthTokenAsync()
    {
        var formData = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "scope", string.Join(" ", TetScopes) },
        };

        var content = new FormUrlEncodedContent(formData);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/token")
        {
            Content = content,
        };

        const string authString = $"{TestClientId}:{TestClientSecret}";
        var base64Auth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(authString));

        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64Auth);

        var response = await fixture.Client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Auth Failed: {response.StatusCode} - {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        return result.GetProperty("access_token").GetString();
    }

    /// <summary>
    /// Uses Poly to keep polling for a Completed message
    /// </summary>
    /// <param name="orchestrationId"></param>
    private async Task WaitForCompletionAsync(string orchestrationId)
    {
        var isCompleted = false;

        var retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r =>
            {
                // Read response, if status is NOT "Completed", keep retrying
                var content = r.Content.ReadAsStringAsync().Result;
                if (content.Contains("Completed"))
                {
                    isCompleted = true;
                }
                return !content.Contains("Completed");
            })
            .WaitAndRetryAsync(
                15,
                retryAttempt =>
                {
                    Console.WriteLine($"Attempt {retryAttempt}");
                    return TimeSpan.FromSeconds(2);
                }
            );

        await retryPolicy.ExecuteAsync(() =>
            fixture.Client.GetAsync($"/api/v1/searches/{orchestrationId}")
        );

        Assert.True(isCompleted);
    }
}
