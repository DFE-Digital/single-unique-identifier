using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using SUI.Find.Application.Models.Matching;

namespace SUI.Find.E2ETests;

[Trait("Category", "E2E")]
[Trait("Suite", "Smoke")]
public class FindSmokeTests(FunctionTestFixture fixture, ITestOutputHelper testOutputHelper)
    : E2ETestBase(fixture, testOutputHelper)
{
    private const string TestClientId = "LOCAL-AUTHORITY-01";
    private const string TestClientSecret = "SUIProject";
    private const int ApiKeyAcceptanceRetryCount = 6;
    private static readonly string[] MatchReadScopes = ["match-record.read"];
    private static readonly TimeSpan ApiKeyAcceptanceRetryInterval = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Should_ReportHealthy()
    {
        await Fixture.EnsureFindApiIsUpAsync(TestOutputHelper);
    }

    [Fact]
    public async Task Should_IssueAuthToken_ForKnownClientCredentials()
    {
        await Fixture.EnsureFindApiIsUpAsync(TestOutputHelper);

        var authToken = await GetAuthTokenAsync(TestClientId, TestClientSecret, MatchReadScopes);

        Assert.False(string.IsNullOrWhiteSpace(authToken));
    }

    [Fact]
    public async Task Should_MatchPerson_WithConfiguredApiKey()
    {
        await Fixture.EnsureFindApiIsUpAsync(TestOutputHelper);

        Assert.False(
            string.IsNullOrWhiteSpace(Fixture.Config.FindApiKey),
            "Smoke tests require E2E__FindApiKey to be set."
        );

        var authToken = await GetAuthTokenAsync(TestClientId, TestClientSecret, MatchReadScopes);

        await WaitForConfiguredApiKeyAcceptanceAsync(authToken!);

        using var response = await PostMatchPersonAsync(
            CreateValidMatchRequest(),
            authToken!,
            logRequestBody: true
        );
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Expected MatchPerson to return OK but got {(int)response.StatusCode} {response.StatusCode}. Response body: {responseBody}"
        );

        var json = JsonSerializer.Deserialize<JsonElement>(responseBody);
        var hasPersonId =
            json.TryGetProperty("PersonId", out var personId)
            || json.TryGetProperty("personId", out personId);

        Assert.True(hasPersonId, "MatchPerson response did not contain PersonId.");
        Assert.False(string.IsNullOrWhiteSpace(personId.GetString()));

        await WaitForPreviousApiKeyRejectionAsync(authToken!);
    }

    private async Task WaitForConfiguredApiKeyAcceptanceAsync(string authToken)
    {
        for (var attempt = 1; attempt <= ApiKeyAcceptanceRetryCount + 1; attempt++)
        {
            try
            {
                using var response = await PostMatchPersonAsync(
                    CreateValidMatchRequest(),
                    authToken
                );
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.Unauthorized)
                {
                    TestOutputHelper.WriteLine(
                        "Configured x-api-key accepted by MatchPerson with status {0}.",
                        response.StatusCode
                    );
                    return;
                }

                if (attempt > ApiKeyAcceptanceRetryCount)
                {
                    Assert.Fail(
                        $"MatchPerson continued to reject the configured x-api-key after rotation. Last response: {(int)response.StatusCode} {response.StatusCode}. Response body: {responseBody}"
                    );
                }

                TestOutputHelper.WriteLine(
                    "Configured x-api-key not accepted yet (attempt {0}/{1}). Waiting {2} seconds before retrying...",
                    attempt,
                    ApiKeyAcceptanceRetryCount + 1,
                    ApiKeyAcceptanceRetryInterval.TotalSeconds
                );
            }
            catch (HttpRequestException ex) when (attempt <= ApiKeyAcceptanceRetryCount)
            {
                TestOutputHelper.WriteLine(
                    "MatchPerson probe failed while waiting for key activation (attempt {0}/{1}): {2}",
                    attempt,
                    ApiKeyAcceptanceRetryCount + 1,
                    ex.Message
                );
            }

            await Task.Delay(ApiKeyAcceptanceRetryInterval);
        }
    }

    private async Task WaitForPreviousApiKeyRejectionAsync(string authToken)
    {
        if (string.IsNullOrWhiteSpace(Fixture.Config.PreviousFindApiKey))
        {
            TestOutputHelper.WriteLine(
                "No previous x-api-key configured; skipping previous key rejection check."
            );
            return;
        }

        Assert.NotEqual(Fixture.Config.FindApiKey, Fixture.Config.PreviousFindApiKey);

        for (var attempt = 1; attempt <= ApiKeyAcceptanceRetryCount + 1; attempt++)
        {
            try
            {
                using var response = await PostMatchPersonAsync(
                    CreateValidMatchRequest(),
                    authToken,
                    apiKey: Fixture.Config.PreviousFindApiKey
                );
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    TestOutputHelper.WriteLine("Previous x-api-key rejected by MatchPerson.");
                    return;
                }

                if (attempt > ApiKeyAcceptanceRetryCount)
                {
                    Assert.Fail(
                        $"MatchPerson continued to accept the previous x-api-key after rotation. Last response: {(int)response.StatusCode} {response.StatusCode}. Response body: {responseBody}"
                    );
                }

                TestOutputHelper.WriteLine(
                    "Previous x-api-key is still accepted (attempt {0}/{1}, status {2}). Waiting {3} seconds before retrying...",
                    attempt,
                    ApiKeyAcceptanceRetryCount + 1,
                    response.StatusCode,
                    ApiKeyAcceptanceRetryInterval.TotalSeconds
                );
            }
            catch (HttpRequestException ex) when (attempt <= ApiKeyAcceptanceRetryCount)
            {
                TestOutputHelper.WriteLine(
                    "MatchPerson probe failed while waiting for previous key rejection (attempt {0}/{1}): {2}",
                    attempt,
                    ApiKeyAcceptanceRetryCount + 1,
                    ex.Message
                );
            }

            await Task.Delay(ApiKeyAcceptanceRetryInterval);
        }
    }

    private async Task<HttpResponseMessage> PostMatchPersonAsync(
        MatchRequest requestBody,
        string authToken,
        string? apiKey = null,
        bool logRequestBody = false
    )
    {
        Fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            authToken
        );
        Fixture.Client.DefaultRequestHeaders.Remove("x-api-key");
        Fixture.Client.DefaultRequestHeaders.Add("x-api-key", apiKey ?? Fixture.Config.FindApiKey);

        var requestJson = JsonSerializer.Serialize(requestBody);
        if (logRequestBody)
        {
            TestOutputHelper.WriteLine("MatchPerson request payload: {0}", requestJson);
        }

        var stringContent = new StringContent(requestJson);
        return await Fixture.Client.PostAsync("v1/matchperson", stringContent);
    }

    private static MatchRequest CreateValidMatchRequest() =>
        new()
        {
            PersonSpecification = new PersonSpecification
            {
                Given = "Octavia",
                Family = "Chislett",
                BirthDate = DateOnly.Parse("2008-09-20"),
            },
            Metadata = [new Metadata { RecordType = "personal.details", RecordId = "9691292211" }],
        };
}
