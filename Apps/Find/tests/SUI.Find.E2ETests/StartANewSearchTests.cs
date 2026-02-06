using System.IO.Compression;
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
using static System.Environment;

namespace SUI.Find.E2ETests;

/// <summary>
/// These tests are designed around the mock data files we use in dev/test environments.
/// <para>See SUI.Find.Infrastructure/Data/auth-clients.json for details</para>
/// </summary>
[Collection("E2E")]
[Trait("Category", "E2E")]
public class StartANewSearchTests(FunctionTestFixture fixture, ITestOutputHelper testOutputHelper)
    : E2ETestBase(fixture, testOutputHelper),
        IClassFixture<FunctionTestFixture>,
        IAsyncLifetime
{
    private const string TestClientId = "LOCAL-AUTHORITY-01";
    private const string TestClientSecret = "SUIProject";
    private static readonly string[] TetScopes =
    [
        "find-record.write",
        "find-record.read",
        "fetch-record.write",
        "fetch-record.read",
    ];
    private const string ValidEncryptedSuid = "JOUlb4UYZy5LiU5_aZECcA"; // Test id that exists in mock data

    private record HealthCheckResponse(string? Value);

    private record SearchStatusResponse(string? Status);

    public async Task InitializeAsync()
    {
        if (!Fixture.Config.SkipResetAzureTables)
        {
            await ResetAzureTablesAsync([
                "ResultsUrlMappings",
                "TestHubNameHistory",
                "TestHubNameInstances",
            ]);
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task ResetAzureTablesAsync(string[] tableNames)
    {
        var service = new TableServiceClient(Fixture.Config.FindApiStorageConnectionString);

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

        TestOutputHelper.WriteLine($"Reset Azure Tables complete: {string.Join(", ", tableNames)}");
    }

    [Fact]
    public async Task Should_PersistSearchData_When_OrchestrationCompletes()
    {
        await EnsureApiIsUpAsync();

        var authToken = await GetAuthTokenAsync(TestClientId, TestClientSecret, TetScopes);
        Fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
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

    private async Task EnsureApiIsUpAsync()
    {
        const string url = "health";
        var waitInterval = TimeSpan.FromSeconds(10);

        TestOutputHelper.WriteLine($"Checking Find API is up: {Fixture.Client.BaseAddress}{url}");

        // If health check does not indicate healthy, wait and then retry
        const int retryCount = 3;
        var retryPolicy = Policy
            .HandleResult<bool>(healthy => !healthy)
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt =>
                {
                    TestOutputHelper.WriteLine(
                        $"Find API does not indicate healthy, waiting for {waitInterval.Seconds} seconds, then retrying, retry {retryAttempt} / {retryCount}..."
                    );
                    return TimeSpan.FromSeconds(10);
                }
            );

        var healthy = await retryPolicy.ExecuteAsync(async () =>
        {
            try
            {
                using var response = await Fixture.Client.GetAsync(url);
                var content = response.Content.ReadFromJsonAsync<HealthCheckResponse>().Result;
                return content?.Value == "Healthy";
            }
            catch (Exception ex)
            {
                TestOutputHelper.WriteLine($"Warning: health check exception: {ex.Message}");
                return false;
            }
        });

        Assert.True(healthy, "The Find API does not appear to be up and healthy");

        TestOutputHelper.WriteLine("Find API is up 👍");
    }

    private async Task<SearchJob> RunAndAssertNewSearchEndpoint()
    {
        const string startSearchUrl = "v1/searches";

        TestOutputHelper.WriteLine(
            $"Starting new search to find records for SUI: {ValidEncryptedSuid} ({Fixture.Client.BaseAddress}{startSearchUrl})"
        );

        var body = new StartSearchRequest(ValidEncryptedSuid);
        var stringContent = new StringContent(JsonSerializer.Serialize(body));
        var newSearchJobResult = await Fixture.Client.PostAsync(startSearchUrl, stringContent);

        // We then want to assert the returned body and status code
        Assert.Equal(HttpStatusCode.Accepted, newSearchJobResult.StatusCode);
        var searchJobContent = await newSearchJobResult.Content.ReadAsStringAsync();
        var searchJob = JsonSerializer.Deserialize<SearchJob>(searchJobContent);
        Assert.False(string.IsNullOrEmpty(searchJob!.JobId));

        var traceId =
            (
                newSearchJobResult.Headers.TryGetValues("Trace-Id", out var traceIds)
                    ? traceIds.FirstOrDefault()
                    : null
            ) ?? "Unknown";

        var invocationId =
            (
                newSearchJobResult.Headers.TryGetValues("Invocation-Id", out var invocationIds)
                    ? invocationIds.FirstOrDefault()
                    : null
            ) ?? "Unknown";

        TestOutputHelper.WriteLine(
            $"Search started: {new { traceId, invocationId, jobId = searchJob.JobId }}"
        );

        var observabilityInfo = Fixture.Config.IsLocal
            ? $"http://localhost:18888/structuredlogs?filters=log.traceid%3Aequals%3A{traceId}"
            : GenerateAppInsightsLink(traceId);
        TestOutputHelper.WriteLine($"{NewLine}Trace observability: {observabilityInfo}{NewLine}");

        return searchJob;
    }

    private async Task RunAndAssertFetchEndpoint(string url)
    {
        url = RemoveLeadingSlashFromUrl(url);

        TestOutputHelper.WriteLine(
            $"Getting search results from: {Fixture.Client.BaseAddress}{url}"
        );

        var searchResults = await Fixture.Client.GetAsync(url);
        // Look for URL link and save as variable
        var searchResultContent = await searchResults.Content.ReadAsStringAsync();
        var searchResultTypedContent = JsonSerializer.Deserialize<SearchResults>(
            searchResultContent
        );
        Assert.True(searchResultTypedContent!.Items.Length == 5);
        foreach (var searchResultItem in searchResultTypedContent.Items)
        {
            Assert.False(string.IsNullOrEmpty(searchResultItem.RecordUrl));

            // TODO: Step 4, Get data from fetch and assert
            var recordUrl = RemoveLeadingSlashFromUrl(searchResultItem.RecordUrl);
            TestOutputHelper.WriteLine(
                $"Getting data from fetch: {Fixture.Client.BaseAddress}{recordUrl}"
            );

            var fetchResult = await Fixture.Client.GetAsync(recordUrl);
            Assert.Equal(HttpStatusCode.OK, fetchResult.StatusCode);
            var fetchResultContent = await fetchResult.Content.ReadAsStringAsync();
            var fetchResultTypedContent = JsonSerializer.Deserialize<CustodianRecord>(
                fetchResultContent
            );
            Assert.False(string.IsNullOrEmpty(fetchResultTypedContent!.RecordId));
            Assert.False(string.IsNullOrEmpty(fetchResultTypedContent.PersonId));
            Assert.False(string.IsNullOrEmpty(fetchResultTypedContent.RecordType));
            Assert.False(string.IsNullOrEmpty(fetchResultTypedContent.SchemaUri));
            Assert.NotNull(fetchResultTypedContent.Payload);

            TestOutputHelper.WriteLine("Fetch verified ok");

            switch (fetchResultTypedContent.RecordType)
            {
                case "health.details":
                    var registeredGpName = fetchResultTypedContent.Payload.Value.GetProperty(
                        "registeredGPName"
                    );
                    Assert.Equal("Dr E Green", registeredGpName.GetString());
                    break;
                case "childrens-services.details":
                    var keyWorker = fetchResultTypedContent.Payload.Value.GetProperty("keyWorker");
                    Assert.Equal("Alex Patel", keyWorker.GetString());
                    break;
                case "education.details":
                    var educationSettingName = fetchResultTypedContent.Payload.Value.GetProperty(
                        "educationSettingName"
                    );
                    Assert.Equal("ST Johns", educationSettingName.GetString());
                    break;
                case "personal.details":
                    var firstName = fetchResultTypedContent.Payload.Value.GetProperty("firstName");
                    Assert.Equal("Octavia", firstName.GetString());
                    break;
                case "crime-justice.details":
                    var policeMarkerDetails = fetchResultTypedContent.Payload.Value.GetProperty(
                        "policeMarkerDetails"
                    );
                    Assert.Equal(
                        "Individuals at the address may resort to violent behaviour",
                        policeMarkerDetails.GetString()
                    );
                    break;
            }
        }
    }

    /// <summary>
    /// Uses Poly to keep polling for a Completed message
    /// </summary>
    /// <param name="url">URL of the Search Status endpoint</param>
    private async Task<SearchJob> RunAndAwaitAndAssertSearchStatusCompletion(string url)
    {
        url = RemoveLeadingSlashFromUrl(url);

        const int retryCount = 30;

        var isCompleted = false;
        var status = "unknown";

        TestOutputHelper.WriteLine(
            $"Checking for search completion: {Fixture.Client.BaseAddress}{url}"
        );

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

                var retry = status is "Queued" or "Running";
                return retry;
            })
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt =>
                {
                    TestOutputHelper.WriteLine(
                        $"Still checking for search completion, status was {status}, retry {retryAttempt} / {retryCount}..."
                    );
                    return TimeSpan.FromSeconds(2);
                }
            );

        await retryPolicy.ExecuteAsync(() => Fixture.Client.GetAsync(url));

        TestOutputHelper.WriteLine(
            $"Finished checking for search completion, final status was {status}, is completed: {isCompleted}"
        );

        Assert.True(isCompleted);

        var typedResult = await Fixture.Client.GetFromJsonAsync<SearchJob>(url);
        return typedResult!;
    }

    private static string RemoveLeadingSlashFromUrl(string url) =>
        url.StartsWith('/') ? url[1..] : url;

    private static string GenerateAppInsightsLink(string traceId)
    {
        const string template =
            "https://portal.azure.com#@fad277c9-c60a-4da1-b5f3-b3b8b34a82f9/blade/Microsoft_OperationsManagementSuite_Workspace/Logs.ReactView/resourceId/%2Fsubscriptions%2F4be6cd11-d358-413e-a744-8716ef3488c8%2FresourceGroups%2Fs270d01rg-ukw-dev%2Fproviders%2Fmicrosoft.insights%2Fcomponents%2Fs270d01appi-ukw-services01/source/LogsBlade.AnalyticsShareLinkToQuery/q/{0}/timespan/P1D/limit/1000";

        var query = $"""
            union traces, exceptions
            | where operation_Id == "{traceId}"
            | extend message = coalesce(message, innermostMessage)
            """;

        return string.Format(template, EncodedKqlQuery(query));

        static string EncodedKqlQuery(string query)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(query);
            using var memoryStream = new MemoryStream();
            using (
                var compressedStream = new GZipStream(
                    memoryStream,
                    CompressionMode.Compress,
                    leaveOpen: true
                )
            )
            {
                compressedStream.Write(bytes, 0, bytes.Length);
            }
            memoryStream.Seek(0, SeekOrigin.Begin);
            var data = memoryStream.ToArray();
            var encodedQuery = Convert.ToBase64String(data);
            return System.Web.HttpUtility.UrlEncode(encodedQuery);
        }
    }
}
