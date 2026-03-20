using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Polly;
using SUI.Find.Application.Enums;
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
[Trait("Suite", "Standard")]
public class StartANewSearchTests(FunctionTestFixture fixture, ITestOutputHelper testOutputHelper)
    : E2ETestBase(fixture, testOutputHelper),
        IAsyncLifetime
{
    private const string TestClientSecret = "SUIProject";
    private static readonly string[] TestScopes =
    [
        "find-record.write",
        "find-record.read",
        "fetch-record.write",
        "fetch-record.read",
    ];

    private record SearchStatusResponse(string? Status);

    public async Task InitializeAsync()
    {
        await Fixture.EnsureServicesAreUpAsync(TestOutputHelper);

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

    public static TheoryData<TestData> TestData =>
        [
            new()
            {
                EncryptedSui = "gkITssvF1IAbNgpcMv2lyA",
                Sui = "9691292211",
                TestClientId = "LOCAL-AUTHORITY-01",
                Records =
                [
                    new TestRecord { RecordType = "health.details", TestValue = "Dr E Green" },
                    new TestRecord
                    {
                        RecordType = "childrens-services.details",
                        TestValue = "Alex Patel",
                    },
                    new TestRecord { RecordType = "education.details", TestValue = "ST Johns" },
                    new TestRecord { RecordType = "personal.details", TestValue = "Octavia" },
                    new TestRecord { RecordType = "personal.details", TestValue = "Octavia" },
                    new TestRecord
                    {
                        RecordType = "crime-justice.details",
                        TestValue = "Individuals at the address may resort to violent behaviour",
                    },
                    new TestRecord
                    {
                        RecordType = "crime-justice.details",
                        TestValue = "Risk of home visits - dangerous dog reported",
                    },
                ],
            },
            new()
            {
                EncryptedSui = "vehNMF2ySUU23P206A6BYA",
                Sui = "9691292211",
                TestClientId = "EDUCATION-01",
                Records =
                [
                    new TestRecord { RecordType = "health.details", TestValue = "Dr E Green" },
                    new TestRecord
                    {
                        RecordType = "childrens-services.details",
                        TestValue = "Alex Patel",
                    },
                    new TestRecord { RecordType = "education.details", TestValue = "ST Johns" },
                    new TestRecord { RecordType = "personal.details", TestValue = "Octavia" },
                    new TestRecord { RecordType = "personal.details", TestValue = "Octavia" },
                ],
            },
            new()
            {
                EncryptedSui = "-hg7DkXLL7oqmKzPwAfxGA",
                Sui = "9449306613",
                TestClientId = "LOCAL-AUTHORITY-01",
                Records = [new TestRecord { RecordType = "personal.details", TestValue = "Briar" }],
            },
            new()
            {
                EncryptedSui = "Gwy1RFyGF4b_sSbbPZExtQ",
                Sui = "9449306494",
                TestClientId = "HEALTH-01",
                Records =
                [
                    new TestRecord { RecordType = "personal.details", TestValue = "Red" },
                    new TestRecord { RecordType = "health.details", TestValue = "Dr E Green" },
                ],
            },
        ];

    [Theory]
    [MemberData(nameof(TestData))]
    [SuppressMessage(
        "Usage",
        "xUnit1045",
        Justification = "The `TestData` is a C# record, and the default string serialization of records provides distinct text for the purposes of test exploration, identification and results."
    )]
    public async Task Should_PersistSearchData_When_OrchestrationCompletes(TestData testData)
    {
        var authToken = await GetAuthTokenAsync(
            testData.TestClientId,
            TestClientSecret,
            TestScopes
        );
        Fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            authToken
        );

        // Step 1, start a new search
        var newSearchJob = await RunAndAssertNewSearchEndpoint(
            Fixture.Config.UseEncryptedIds ? testData.EncryptedSui : testData.Sui
        );

        var hasStatusLink = newSearchJob.Links.TryGetValue("status", out var statusLink);
        Assert.True(hasStatusLink);

        var hasResultsLink = newSearchJob.Links.TryGetValue("results", out var resultsLink);
        Assert.True(hasResultsLink);

        // Step 2, check the status
        var statusResult = await RunAndAwaitAndAssertSearchStatusCompletion(
            statusLink!.Href,
            resultsLink!.Href,
            testData
        );
        Assert.NotNull(statusResult);

        // Step 3, Get the results from fetch and assert
        await RunAndAssertFetchEndpoints(resultsLink.Href, testData);

        // Fin
    }

    private async Task<SearchJob> RunAndAssertNewSearchEndpoint(string suid)
    {
        const string startSearchUrl = "v1/searches";

        TestOutputHelper.WriteLine(
            $"Starting new search to find records for SUI: {suid} ({Fixture.Client.BaseAddress}{startSearchUrl})"
        );

        var body = new StartSearchRequest(suid);
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

        var observabilityLink = Fixture.Config.IsLocal
            ? $"http://localhost:18888/structuredlogs?filters=log.traceid%3Aequals%3A{traceId}"
            : GenerateAppInsightsLink(traceId);

        TestOutputHelper.WriteLine("");
        TestOutputHelper.WriteLine($"Trace observability: {observabilityLink}");
        TestOutputHelper.WriteLine("");

        return searchJob;
    }

    private async Task RunAndAssertPartialSearchResults(string url, TestData testData)
    {
        url = RemoveLeadingSlashFromUrl(url);

        TestOutputHelper.WriteLine(
            $"Getting partial search results from: {Fixture.Client.BaseAddress}{url}"
        );

        var searchResults = await Fixture.Client.GetAsync(url);
        var searchResultContent = await searchResults.Content.ReadAsStringAsync();
        var searchResultTypedContent = JsonSerializer.Deserialize<SearchResults>(
            searchResultContent
        );

        Assert.NotNull(searchResultTypedContent);

        if (searchResultTypedContent.Status != SearchStatus.Running)
        {
            return; // Early exit if the job is no longer running
        }

        // Verify that we haven't got more results than we should (verifies that previous search results for this SUI+Custodian aren't being included)
        Assert.InRange(searchResultTypedContent.Items.Length, 0, testData.Records.Length);

        // Verify that PEP filtering has been applied to the partial search results, by checking we only have the record types that we're expecting
        var expectedRecordTypes = testData.Records.Select(r => r.RecordType).ToFrozenSet();
        var actualRecordTypes = searchResultTypedContent
            .Items.Select(item => item.RecordType)
            .ToFrozenSet();

        var invalidRecordTypes = actualRecordTypes.Except(expectedRecordTypes).ToArray();
        Assert.Empty(invalidRecordTypes);
    }

    private async Task RunAndAssertFetchEndpoints(string url, TestData testData)
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
        Assert.True(
            searchResultTypedContent!.Items.Length == testData.Records.Length,
            $"Record count mismatch. Expected: {testData.Records.Length}, actual: {searchResultTypedContent.Items.Length}"
        );
        foreach (var searchResultItem in searchResultTypedContent.Items)
        {
            Assert.False(string.IsNullOrEmpty(searchResultItem.RecordUrl));

            // Step 4, Get data from fetch and assert
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
                    Assert.Equal(
                        testData.Records.First(x => x.RecordType == "health.details").TestValue,
                        registeredGpName.GetString()
                    );
                    break;
                case "childrens-services.details":
                    var keyWorker = fetchResultTypedContent.Payload.Value.GetProperty("keyWorker");
                    Assert.Equal(
                        testData
                            .Records.First(x => x.RecordType == "childrens-services.details")
                            .TestValue,
                        keyWorker.GetString()
                    );
                    break;
                case "education.details":
                    var educationSettingName = fetchResultTypedContent.Payload.Value.GetProperty(
                        "educationSettingName"
                    );
                    Assert.Equal(
                        testData.Records.First(x => x.RecordType == "education.details").TestValue,
                        educationSettingName.GetString()
                    );
                    break;
                case "personal.details":
                    var firstName = fetchResultTypedContent.Payload.Value.GetProperty("firstName");
                    Assert.Equal(
                        testData.Records.First(x => x.RecordType == "personal.details").TestValue,
                        firstName.GetString()
                    );
                    break;
                case "crime-justice.details":
                    var policeMarkerDetails = fetchResultTypedContent.Payload.Value.GetProperty(
                        "policeMarkerDetails"
                    );
                    Assert.Contains(
                        policeMarkerDetails.GetString(),
                        testData
                            .Records.Where(x => x.RecordType == "crime-justice.details")
                            .Select(x => x.TestValue)
                    );
                    break;
            }
        }
    }

    /// <summary>
    /// Uses Poly to keep polling for a Completed message
    /// </summary>
    /// <param name="statusUrl">URL of the Search Status endpoint</param>
    /// <param name="resultsUrl">URL of the Search Results endpoint</param>
    /// <param name="testData">The test data for this search run</param>
    private async Task<SearchJob> RunAndAwaitAndAssertSearchStatusCompletion(
        string statusUrl,
        string resultsUrl,
        TestData testData
    )
    {
        statusUrl = RemoveLeadingSlashFromUrl(statusUrl);

        const int retryCount = 150;

        var isCompleted = false;
        var status = "unknown";

        TestOutputHelper.WriteLine(
            $"Checking for search completion: {Fixture.Client.BaseAddress}{statusUrl}"
        );

        var retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r =>
            {
                if (!r.IsSuccessStatusCode)
                {
                    status = $"{r.StatusCode} - {r.Content.ReadAsStringAsync().Result}";
                    return true; // i.e. retry
                }

                // Read response, if status is NOT "Completed", keep retrying
                var content = r.Content.ReadFromJsonAsync<SearchStatusResponse>().Result;
                status = content?.Status;
                switch (status)
                {
                    case "Completed":
                        isCompleted = true;
                        break;
                    case "Running":
                        // Verify partial results are as expected while its in-progress
                        RunAndAssertPartialSearchResults(resultsUrl, testData).Wait();
                        break;
                }

                var retry = status is "Queued" or "Running";
                return retry;
            })
            .Or<Exception>(ex =>
            {
                var exceptions = new List<Exception>([ex]);
                while (ex.InnerException != null)
                {
                    exceptions.Add(ex.InnerException);
                    ex = ex.InnerException;
                }

                var isTimeout = exceptions.OfType<TimeoutException>().Any();
                if (isTimeout)
                {
                    status = "(timeout)";
                }

                return isTimeout; // i.e. retry if we ever receive a timeout
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

        await retryPolicy.ExecuteAsync(() => Fixture.Client.GetAsync(statusUrl));

        TestOutputHelper.WriteLine(
            $"Finished checking for search completion, final status was {status}, is completed: {isCompleted}"
        );

        Assert.True(isCompleted);

        var typedResult = await Fixture.Client.GetFromJsonAsync<SearchJob>(statusUrl);
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

public class TestData
{
    public required string Sui { get; set; }
    public required TestRecord[] Records { get; set; }
    public required string TestClientId { get; set; }
    public required string EncryptedSui { get; set; }
}

public class TestRecord
{
    public required string RecordType { get; set; }
    public required string TestValue { get; set; }
}
