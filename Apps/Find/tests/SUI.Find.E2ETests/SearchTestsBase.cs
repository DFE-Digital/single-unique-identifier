using System.Collections.Frozen;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Polly;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Models;
using SUI.Find.FindApi.Models;

namespace SUI.Find.E2ETests;

/// <summary>
/// These tests are designed around the mock data files we use in dev/test environments.
/// <para>See Data/auth-clients-inbound.json for details</para>
/// </summary>
[Trait("Category", "E2E")]
[Trait("Suite", "Standard")]
public abstract class SearchTestsBase(
    FunctionTestFixture fixture,
    ITestOutputHelper testOutputHelper
) : E2ETestBase(fixture, testOutputHelper), IAsyncLifetime
{
    protected abstract bool UsePolling { get; }

    private const string TestClientSecret = "SUIProject";
    private static readonly string[] TestScopes =
    [
        "find-record.write",
        "find-record.read",
        "fetch-record.write",
        "fetch-record.read",
    ];

    private record SearchStatusResponse(string? Status, int? CompletenessPercentage);

    /// <summary>
    /// Runs before each individual test
    /// </summary>
    public async ValueTask InitializeAsync() =>
        await Fixture.EnsureServicesAreUpAsync(TestOutputHelper);

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    public static TheoryData<TestData> HappyPathTestData =>
        [
            new TestData
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
            new TestData
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
            new TestData
            {
                EncryptedSui = "-hg7DkXLL7oqmKzPwAfxGA",
                Sui = "9449306613",
                TestClientId = "LOCAL-AUTHORITY-01",
                Records = [new TestRecord { RecordType = "personal.details", TestValue = "Briar" }],
            },
            new TestData
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

    public static TheoryData<TestData> NoRecordsTestData =>
        [
            new TestData
            {
                EncryptedSui = "DcYc-jumZgryOtz3iFh7cw",
                Sui = "9693821998",
                TestClientId = "LOCAL-AUTHORITY-01",
                Records = [],
            },
            new TestData
            {
                EncryptedSui = "ZBLNLdIppgMge_MmzVImmA",
                Sui = "9691292211",
                TestClientId = "NO-DSA-01",
                Records = [],
            },
        ];

    protected async Task RunTest(TestData testData)
    {
        var authToken = await GetAuthTokenAsync(
            testData.TestClientId,
            TestClientSecret,
            TestScopes
        );

        if (string.IsNullOrWhiteSpace(authToken))
        {
            Assert.Fail("Auth token could not be retrieved.");
        }

        // Step 1, start a new search
        var searchJobLinks = await RunAndAssertNewSearchEndpoint(
            Fixture.Config.UseEncryptedIds ? testData.EncryptedSui : testData.Sui,
            authToken
        );

        var hasStatusLink = searchJobLinks.TryGetValue("status", out var statusLink);
        if (!UsePolling)
        {
            Assert.True(hasStatusLink);
        }

        var hasResultsLink = searchJobLinks.TryGetValue("results", out var resultsLink);
        Assert.True(hasResultsLink);

        // Step 2, check the status
        await RunAndAwaitAndAssertSearchStatusCompletion(
            statusLink != null ? statusLink.Href : resultsLink!.Href,
            resultsLink!.Href,
            testData,
            authToken
        );

        // Step 3, Get the results from fetch and assert
        await RunAndAssertFetchEndpoints(resultsLink.Href, testData, authToken);

        // Fin
    }

    protected async Task<Dictionary<string, HalLink>> RunAndAssertNewSearchEndpoint(
        string suid,
        string authToken
    )
    {
        var startSearchUrl = UsePolling ? "v2/searches" : "v1/searches";

        TestOutputHelper.WriteLine(
            $"Starting new search to find records for SUI: {suid} ({Fixture.Client.BaseAddress}{startSearchUrl})"
        );

        var body = new StartSearchRequest(suid);

        using var request = new HttpRequestMessage(HttpMethod.Post, startSearchUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        using var newSearchJobResult = await Fixture.Client.SendAsync(request);

        // We then want to assert the returned body and status code
        Assert.Equal(HttpStatusCode.Accepted, newSearchJobResult.StatusCode);
        var searchJobContent = await newSearchJobResult.Content.ReadAsStringAsync();
        var searchId = string.Empty;
        var links = new Dictionary<string, HalLink>();

        if (UsePolling)
        {
            var searchJob = JsonSerializer.Deserialize<SearchJobV2>(searchJobContent);
            if (searchJob != null)
            {
                searchId = searchJob.WorkItemId;
                links = searchJob.Links;
            }
        }
        else
        {
            var searchJob = JsonSerializer.Deserialize<SearchJob>(searchJobContent);
            if (searchJob != null)
            {
                searchId = searchJob.JobId;
                links = searchJob.Links;
            }
        }

        Assert.False(string.IsNullOrEmpty(searchId));

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

        var topLevelTaskName = UsePolling ? "workItemId" : "jobId";
        TestOutputHelper.WriteLine(
            $"Search started: traceId={traceId}, invocationId={invocationId}, {topLevelTaskName}={searchId}"
        );

        var observabilityLink = Fixture.Config.IsLocal
            ? $"http://localhost:18888/structuredlogs?filters=log.traceid%3Aequals%3A{traceId}"
            : GenerateAppInsightsLink(traceId);

        TestOutputHelper.WriteLine("");
        TestOutputHelper.WriteLine($"Trace observability: {observabilityLink}");
        TestOutputHelper.WriteLine("");

        return links;
    }

    protected async Task RunAndAssertPartialSearchResults(
        string url,
        TestData testData,
        string authToken
    )
    {
        url = RemoveLeadingSlashFromUrl(url);

        TestOutputHelper.WriteLine(
            $"Getting partial search results from: {Fixture.Client.BaseAddress}{url}"
        );

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        using var searchResults = await Fixture.Client.SendAsync(request);
        var searchResultContent = await searchResults.Content.ReadAsStringAsync();

        var searchResultTypedContent = JsonSerializer.Deserialize<SearchResultsBase>(
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

    protected async Task RunAndAssertFetchEndpoints(string url, TestData testData, string authToken)
    {
        url = RemoveLeadingSlashFromUrl(url);

        TestOutputHelper.WriteLine(
            $"Getting search results from: {Fixture.Client.BaseAddress}{url}"
        );

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        using var searchResults = await Fixture.Client.SendAsync(request);

        // Look for URL link and save as variable
        var searchResultContent = await searchResults.Content.ReadAsStringAsync();
        var searchResultTypedContent = JsonSerializer.Deserialize<SearchResultsBase>(
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

            using var fetchRequest = new HttpRequestMessage(HttpMethod.Get, recordUrl);
            fetchRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            using var fetchResult = await Fixture.Client.SendAsync(fetchRequest);

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

            var payload = fetchResultTypedContent.Payload!.Value;

            switch (fetchResultTypedContent.RecordType)
            {
                case "health.details":
                    var registeredGpName = payload.GetProperty("registeredGPName");
                    Assert.Equal(
                        testData.Records.First(x => x.RecordType == "health.details").TestValue,
                        registeredGpName.GetString()
                    );
                    break;
                case "childrens-services.details":
                    var keyWorker = payload.GetProperty("keyWorker");
                    Assert.Equal(
                        testData
                            .Records.First(x => x.RecordType == "childrens-services.details")
                            .TestValue,
                        keyWorker.GetString()
                    );
                    break;
                case "education.details":
                    var educationSettingName = payload.GetProperty("educationSettingName");
                    Assert.Equal(
                        testData.Records.First(x => x.RecordType == "education.details").TestValue,
                        educationSettingName.GetString()
                    );
                    break;
                case "personal.details":
                    var firstName = payload.GetProperty("firstName");
                    Assert.Equal(
                        testData.Records.First(x => x.RecordType == "personal.details").TestValue,
                        firstName.GetString()
                    );
                    break;
                case "crime-justice.details":
                    var policeMarkerDetails = payload.GetProperty("policeMarkerDetails");
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
    /// <param name="authToken">AuthToken for this search run</param>
    protected async Task RunAndAwaitAndAssertSearchStatusCompletion(
        string statusUrl,
        string resultsUrl,
        TestData testData,
        string authToken
    )
    {
        statusUrl = RemoveLeadingSlashFromUrl(statusUrl);

        var timeout = TimeSpan.FromMinutes(UsePolling ? 10 : 5);
        var retryDelay = TimeSpan.FromSeconds(2);
        var retryCount = (int)Math.Round(timeout / retryDelay);

        var isCompleted = false;
        var statusMessage = "unknown";

        TestOutputHelper.WriteLine(
            $"Checking for search completion: {Fixture.Client.BaseAddress}{statusUrl}"
        );

        var retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r =>
            {
                if (!r.IsSuccessStatusCode)
                {
                    statusMessage = $"{r.StatusCode} - {r.Content.ReadAsStringAsync().Result}";
                    return true; // i.e. retry
                }

                // Read response, if status is NOT "Completed", keep retrying
                var content = r.Content.ReadFromJsonAsync<SearchStatusResponse>().Result;
                var status = content?.Status;

                switch (status)
                {
                    case "Completed":
                        isCompleted = true;
                        break;
                    case "Running":
                        // Verify partial results are as expected while its in-progress
                        RunAndAssertPartialSearchResults(resultsUrl, testData, authToken).Wait();
                        break;
                }

                var retry = status is "Queued" or "Running";

                statusMessage =
                    content?.CompletenessPercentage != null
                        ? $"{status} ({content.CompletenessPercentage}%)"
                        : status;

                return retry;
            })
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt =>
                {
                    TestOutputHelper.WriteLine(
                        $"Still checking for search completion, status was {statusMessage}, retry {retryAttempt} / {retryCount}..."
                    );
                    return retryDelay;
                }
            );

        // Execute the policy with a fresh request object each time to avoid 'ObjectDisposedException'
        await retryPolicy.ExecuteAsync(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, statusUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            return await Fixture.Client.SendAsync(request);
        });

        TestOutputHelper.WriteLine(
            $"Finished checking for search completion, final status was {statusMessage}, is completed: {isCompleted}"
        );

        Assert.True(isCompleted);

        // Final fetch to assert the typed result mapping
        using var finalRequest = new HttpRequestMessage(HttpMethod.Get, statusUrl);
        finalRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        using var finalResponse = await Fixture.Client.SendAsync(finalRequest);

        object? typedResult = UsePolling
            ? await finalResponse.Content.ReadFromJsonAsync<SearchJobV2>()
            : await finalResponse.Content.ReadFromJsonAsync<SearchJob>();

        Assert.NotNull(typedResult);
    }

    protected static string RemoveLeadingSlashFromUrl(string url) =>
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
