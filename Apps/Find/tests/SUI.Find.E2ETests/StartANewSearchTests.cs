using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Polly;
using SUI.Find.Application.Models;
using SUI.Find.Domain.ValueObjects;
using SUI.Find.FindApi.Models;

namespace SUI.Find.E2ETests;

public class StartANewSearchTests(FunctionTestFixture fixture) : IClassFixture<FunctionTestFixture>
{
    public const string TestClientId = "LOCAL-AUTHORITY-01";
    public const string TestClientSecret = "SUIProject";
    public static readonly string[] TetScopes =
    [
        "find-record.write",
        "find-record.read",
        "fetch-record.write",
        "fetch-record.read",
    ];
    private const string ValidEncryptedSuid = "Cy13hyZL-4LSIwVy50p-Hg";

    [Fact]
    public async Task Should_PersistSearchData_When_OrchestrationCompletes()
    {
        var authToken = await GetAuthTokenAsync();
        var body = new StartSearchRequest(ValidEncryptedSuid);
        var stringContent = new StringContent(JsonSerializer.Serialize(body));
        fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            authToken
        );
        var newSearchJobResult = await fixture.Client.PostAsync("v1/searches", stringContent);

        // We then want to assert the returned body and status code
        Assert.Equal(HttpStatusCode.Accepted, newSearchJobResult.StatusCode);
        var searchJobContent = await newSearchJobResult.Content.ReadAsStringAsync();
        var typedContent = JsonSerializer.Deserialize<SearchJob>(searchJobContent);
        Assert.False(string.IsNullOrEmpty(typedContent!.JobId));

        // Step 2, check the status
        var statusResult = await WaitForCompletionAsync(typedContent.JobId);

        //TODO:  Step 3, Get the results and assert
        await RunAndAssertFetchEndpoint(
            statusResult.Links.FirstOrDefault(x => x.Key == "results").Value.Href[1..]
        );

        // Fin
    }

    private async Task RunAndAssertFetchEndpoint(string url)
    {
        var searchResults = await fixture.Client.GetAsync(url);
        // Look for URL link and save as variable
        var searchResultContent = await searchResults.Content.ReadAsStringAsync();
        var searchResultTypedContent = JsonSerializer.Deserialize<SearchResults>(
            searchResultContent
        );
        Assert.Single(searchResultTypedContent!.Items);
        var searchResultItem = searchResultTypedContent.Items[0];
        Assert.False(string.IsNullOrEmpty(searchResultItem.RecordUrl));

        // TODO: Step 4, Get data from fetch and assert
        Assert.Equal(
            $"v1/records/{searchResultItem.RecordUrl.Split("/").Last()}",
            searchResultItem.RecordUrl[1..]
        );
        var fetchResult = await fixture.Client.GetAsync(searchResultItem.RecordUrl[1..]);
        Assert.Equal(HttpStatusCode.OK, fetchResult.StatusCode);
        var fetchResultContent = await fetchResult.Content.ReadAsStringAsync();
        var fetchResultTypedContent = JsonSerializer.Deserialize<CustodianRecord>(
            fetchResultContent
        );
        Assert.False(string.IsNullOrEmpty(fetchResultTypedContent!.RecordId));
        Assert.False(string.IsNullOrEmpty(fetchResultTypedContent.PersonId));
        Assert.False(string.IsNullOrEmpty(fetchResultTypedContent.RecordType));
        Assert.False(string.IsNullOrEmpty(fetchResultTypedContent.SchemaUri));
    }

    private async Task<string?> GetAuthTokenAsync()
    {
        var formData = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "scope", string.Join(" ", TetScopes) },
        };

        var content = new FormUrlEncodedContent(formData);

        var request = new HttpRequestMessage(HttpMethod.Post, "v1/auth/token")
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
    private async Task<SearchJob> WaitForCompletionAsync(string orchestrationId)
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
            fixture.Client.GetAsync($"v1/searches/{orchestrationId}")
        );

        Assert.True(isCompleted);

        var typedResult = await fixture.Client.GetFromJsonAsync<SearchJob>(
            $"v1/searches/{orchestrationId}"
        );
        return typedResult!;
    }
}
