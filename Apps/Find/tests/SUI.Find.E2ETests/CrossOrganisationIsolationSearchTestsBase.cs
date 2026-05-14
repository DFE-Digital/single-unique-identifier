using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using SUI.Find.Application.Models;
using SUI.Find.FindApi.Models;

namespace SUI.Find.E2ETests;

public abstract class CrossOrganisationIsolationSearchTestsBase(
    FunctionTestFixture fixture,
    ITestOutputHelper testOutputHelper
) : SearchTestsBase(fixture, testOutputHelper)
{
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
        Assert.NotEqual(ownerToken, attackerToken);

        var searchJobLinks = await RunAndAssertNewSearchEndpoint(
            Fixture.Config.UseEncryptedIds ? testData.EncryptedSui : testData.Sui,
            ownerToken
        );

        var hasStatusLink = searchJobLinks.TryGetValue("status", out var statusLink);
        var hasResultsLink = searchJobLinks.TryGetValue("results", out var resultsLink);
        var hasCancelLink = searchJobLinks.TryGetValue("cancel", out var cancelLink);

        Assert.True(hasResultsLink);
        var resultsUrl = RemoveLeadingSlashFromUrl(resultsLink!.Href);

        // First, run and wait for the search to complete
        // We need to wait for at least the search to have started running, because some endpoints return not found until the queues have done their bit and eventual consistency has created things
        await RunAndAwaitAndAssertSearchStatusCompletion(
            statusLink?.Href ?? resultsLink.Href,
            resultsLink.Href,
            testData,
            ownerToken
        );

        // Now the search has completed, verify that all the endpoints are not accessible to the attacker
        if (!UsePolling)
        {
            Assert.True(hasStatusLink);
            var statusUrl = RemoveLeadingSlashFromUrl(statusLink!.Href);
            await AssertForbidden(statusUrl, attackerToken, HttpMethod.Get);
        }

        await AssertForbidden(resultsUrl, attackerToken, HttpMethod.Get);

        if (!UsePolling)
        {
            Assert.True(hasCancelLink);
            var cancelUrl = RemoveLeadingSlashFromUrl(cancelLink!.Href);
            await AssertForbidden(cancelUrl, attackerToken, HttpMethod.Delete);
        }

        var searchResultItems = await GetSearchResultItemsAsync(resultsUrl, ownerToken);

        Assert.True(searchResultItems.Length > 0, "No records found to test fetch isolation");
        var recordUrl = RemoveLeadingSlashFromUrl(searchResultItems.First().RecordUrl);
        await AssertForbidden(recordUrl, attackerToken, HttpMethod.Get);
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
                    or HttpStatusCode.NotFound // rs-todo: NotFound should only be for fetch
                    or HttpStatusCode.Unauthorized, // rs-todo: Unauthorized strictly isn't correct
            $"Expected Forbidden, but got {response.StatusCode} for {method} {url}"
        );
    }

    private async Task<SearchResultItem[]> GetSearchResultItemsAsync(
        string resultsUrl,
        string authToken
    )
    {
        using var resultsRequest = new HttpRequestMessage(HttpMethod.Get, resultsUrl);
        resultsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        using var resultsResponse = await Fixture.Client.SendAsync(resultsRequest);
        var resultsContent = await resultsResponse.Content.ReadAsStringAsync();
        var resultsTyped = JsonSerializer.Deserialize<SearchResultsBase>(resultsContent);
        Assert.NotNull(resultsTyped);
        return resultsTyped.Items;
    }
}
