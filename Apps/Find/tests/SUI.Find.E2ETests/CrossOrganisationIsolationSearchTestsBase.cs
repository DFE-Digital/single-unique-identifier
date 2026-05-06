using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using SUI.Find.FindApi.Models;

namespace SUI.Find.E2ETests;

public abstract class CrossOrganisationIsolationSearchTestsBase(
    FunctionTestFixture fixture,
    ITestOutputHelper testOutputHelper
) : SearchTestsBase(fixture, testOutputHelper)
{
    private const string TestClientSecret = "SUIProject";

    private static readonly string[] TestScopes =
    [
        "find-record.write",
        "find-record.read",
        "fetch-record.write",
        "fetch-record.read",
    ];

    protected async Task RunIsolationTest(TestData testData)
    {
        var ownerToken = await GetAuthTokenAsync(
            testData.TestClientId,
            TestClientSecret,
            TestScopes
        );

        var attackerClientId =
            testData.TestClientId == "LOCAL-AUTHORITY-01" ? "EDUCATION-01" : "LOCAL-AUTHORITY-01";

        var attackerToken = await GetAuthTokenAsync(attackerClientId, TestClientSecret, TestScopes);

        Assert.NotNull(ownerToken);
        Assert.NotNull(attackerToken);

        var searchJobLinks = await RunAndAssertNewSearchEndpoint(
            Fixture.Config.UseEncryptedIds ? testData.EncryptedSui : testData.Sui,
            ownerToken
        );

        var hasStatusLink = searchJobLinks.TryGetValue("status", out var statusLink);
        var hasResultsLink = searchJobLinks.TryGetValue("results", out var resultsLink);
        var hasCancelLink = searchJobLinks.TryGetValue("cancel", out var cancelLink);

        Assert.True(hasResultsLink);
        var resultsUrl = RemoveLeadingSlashFromUrl(resultsLink!.Href);

        if (!UsePolling && hasStatusLink)
        {
            var statusUrl = RemoveLeadingSlashFromUrl(statusLink!.Href);
            await AssertForbidden(statusUrl, attackerToken, HttpMethod.Get);
        }

        await AssertForbidden(resultsUrl, attackerToken, HttpMethod.Get);

        if (!UsePolling && hasCancelLink)
        {
            var cancelUrl = RemoveLeadingSlashFromUrl(cancelLink!.Href);
            await AssertForbidden(cancelUrl, attackerToken, HttpMethod.Delete);
        }

        await RunAndAwaitAndAssertSearchStatusCompletion(
            statusLink?.Href ?? resultsLink.Href,
            resultsLink.Href,
            testData,
            ownerToken
        );

        using var resultsRequest = new HttpRequestMessage(HttpMethod.Get, resultsUrl);
        resultsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        using var resultsResponse = await Fixture.Client.SendAsync(resultsRequest);
        var resultsContent = await resultsResponse.Content.ReadAsStringAsync();
        var resultsTyped = JsonSerializer.Deserialize<SearchResultsBase>(resultsContent);

        Assert.NotNull(resultsTyped);
        if (resultsTyped.Items.Length > 0)
        {
            var recordUrl = RemoveLeadingSlashFromUrl(resultsTyped.Items[0].RecordUrl);
            await AssertForbidden(recordUrl, attackerToken, HttpMethod.Get);
        }
        else
        {
            TestOutputHelper.WriteLine("Warning: No records found to test fetch isolation.");
        }
    }

    private async Task AssertForbidden(string url, string token, HttpMethod method)
    {
        TestOutputHelper.WriteLine($"Asserting {method} {url} is forbidden for attacker");
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await Fixture.Client.SendAsync(request);

        Assert.True(
            response.StatusCode
                is HttpStatusCode.Forbidden
                    or HttpStatusCode.NotFound
                    or HttpStatusCode.Unauthorized,
            $"Expected Forbidden or NotFound, but got {response.StatusCode} for {method} {url}"
        );
    }
}
