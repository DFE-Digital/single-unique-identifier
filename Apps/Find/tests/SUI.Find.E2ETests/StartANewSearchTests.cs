using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Polly;
using SUI.Find.Application.Models;
using SUI.Find.FindApi.Models;
using Xunit.Abstractions;

namespace SUI.Find.E2ETests;

/// <summary>
/// These tests are designed around the mock data files we use in dev/test environments.
/// <para>See SUI.Find.Infrastructure/Data/auth-clients.json for details</para>
/// </summary>
[Collection("E2E")]
[Trait("Category", "E2E")]
public class StartANewSearchTests(FunctionTestFixture fixture, ITestOutputHelper testOutputHelper)
    : E2ETestBase(fixture),
        IClassFixture<FunctionTestFixture>,
        IAsyncLifetime
{
    private readonly FunctionTestFixture _fixture = fixture;

    private const string TestClientId = "LOCAL-AUTHORITY-01";
    private const string TestClientSecret = "SUIProject";
    private static readonly string[] TetScopes =
    [
        "find-record.write",
        "find-record.read",
        "fetch-record.write",
        "fetch-record.read",
    ];
    private const string ValidEncryptedSuid = "Cy13hyZL-4LSIwVy50p-Hg"; // Test id that exists in mock data

    private record SearchStatusResponse(string? Status);

    public async Task InitializeAsync()
    {
        await ResetAzureTablesAsync([
            "ResultsUrlMappings",
            "TestHubNameHistory",
            "TestHubNameInstances",
        ]);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task ResetAzureTablesAsync(string[] tableNames)
    {
        var service = new TableServiceClient("UseDevelopmentStorage=true");

        foreach (var table in tableNames)
        {
            try
            {
                await service.DeleteTableAsync(table);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // Table didn't exist; ignore
            }

            await service.CreateTableIfNotExistsAsync(table);
        }
    }

    [Fact]
    public async Task Should_PersistSearchData_When_OrchestrationCompletes()
    {
        var authToken = await GetAuthTokenAsync(TestClientId, TestClientSecret, TetScopes);
        _fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            authToken
        );

        // Step 1, start a new search
        var newSearchJob = await RunAndAssertNewSearchEndpoint();

        // Step 2, check the status
        var statusUrl = newSearchJob.Links.TryGetValue("status", out var statusLink);
        Assert.True(statusUrl);
        var statusResult = await RunAndAwaitAndAssertSearchStatusCompletion(statusLink!.Href);

        // Step 3, Get the results from fetch and assert
        var resultsUrl = statusResult.Links.TryGetValue("results", out var resultsLink);
        Assert.True(resultsUrl);
        await RunAndAssertFetchEndpoint(resultsLink!.Href);

        // Fin
    }

    private async Task<SearchJob> RunAndAssertNewSearchEndpoint()
    {
        var body = new StartSearchRequest(ValidEncryptedSuid);
        var stringContent = new StringContent(JsonSerializer.Serialize(body));
        var newSearchJobResult = await _fixture.Client.PostAsync("v1/searches", stringContent);

        // We then want to assert the returned body and status code
        Assert.Equal(HttpStatusCode.Accepted, newSearchJobResult.StatusCode);
        var searchJobContent = await newSearchJobResult.Content.ReadAsStringAsync();
        var searchJob = JsonSerializer.Deserialize<SearchJob>(searchJobContent);
        Assert.False(string.IsNullOrEmpty(searchJob!.JobId));

        return searchJob;
    }

    private async Task RunAndAssertFetchEndpoint(string url)
    {
        var searchResults = await _fixture.Client.GetAsync(RemoveLeadingSlashFromUrl(url));
        // Look for URL link and save as variable
        var searchResultContent = await searchResults.Content.ReadAsStringAsync();
        var searchResultTypedContent = JsonSerializer.Deserialize<SearchResults>(
            searchResultContent
        );
        Assert.Single(searchResultTypedContent!.Items);
        var searchResultItem = searchResultTypedContent.Items[0];
        Assert.False(string.IsNullOrEmpty(searchResultItem.RecordUrl));

        // TODO: Step 4, Get data from fetch and assert
        var fetchResult = await _fixture.Client.GetAsync(
            RemoveLeadingSlashFromUrl(searchResultItem.RecordUrl)
        );
        Assert.Equal(HttpStatusCode.OK, fetchResult.StatusCode);
        var fetchResultContent = await fetchResult.Content.ReadAsStringAsync();
        var fetchResultTypedContent = JsonSerializer.Deserialize<CustodianRecord>(
            fetchResultContent
        );
        Assert.False(string.IsNullOrEmpty(fetchResultTypedContent!.RecordId));
        Assert.False(string.IsNullOrEmpty(fetchResultTypedContent.PersonId));
        Assert.False(string.IsNullOrEmpty(fetchResultTypedContent.RecordType));
        Assert.False(string.IsNullOrEmpty(fetchResultTypedContent!.SchemaUri));
        Assert.NotNull(fetchResultTypedContent.Payload);
    }

    /// <summary>
    /// Uses Poly to keep polling for a Completed message
    /// </summary>
    /// <param name="url">URL of the Search Status endpoint</param>
    private async Task<SearchJob> RunAndAwaitAndAssertSearchStatusCompletion(string url)
    {
        const int retryCount = 15;

        var isCompleted = false;
        var status = "unknown";

        testOutputHelper.WriteLine("Polling for Search status completion: {0}", url);

        var retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r =>
            {
                // Read response, if status is NOT "Completed", keep retrying
                var content = r.Content.ReadFromJsonAsync<SearchStatusResponse>().Result;
                status = content?.Status;
                if (status == "Completed")
                {
                    isCompleted = true;
                }
                return status != "Completed";
            })
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt =>
                {
                    testOutputHelper.WriteLine(
                        $"Still polling, status was {status}, retry {retryAttempt} / {retryCount}..."
                    );
                    return TimeSpan.FromSeconds(2);
                }
            );

        await retryPolicy.ExecuteAsync(() =>
            _fixture.Client.GetAsync(RemoveLeadingSlashFromUrl(url))
        );

        testOutputHelper.WriteLine(
            $"Finished polling, final status was {status}, is completed: {isCompleted}"
        );

        Assert.True(isCompleted);

        var typedResult = await _fixture.Client.GetFromJsonAsync<SearchJob>(
            RemoveLeadingSlashFromUrl(url)
        );
        return typedResult!;
    }

    private static string RemoveLeadingSlashFromUrl(string url) =>
        url.StartsWith('/') ? url[1..] : url;
}
