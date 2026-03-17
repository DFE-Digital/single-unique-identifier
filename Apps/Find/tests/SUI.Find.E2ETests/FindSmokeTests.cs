using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit.Abstractions;

namespace SUI.Find.E2ETests;

[Collection("E2E")]
[Trait("Category", "Smoke")]
public class FindSmokeTests(FunctionTestFixture fixture, ITestOutputHelper testOutputHelper)
    : E2ETestBase(fixture, testOutputHelper)
{
    private const string TestClientId = "LOCAL-AUTHORITY-01";
    private const string TestClientSecret = "SUIProject";
    private static readonly string[] MatchReadScopes = ["match-record.read"];

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

        var request = new HttpRequestMessage(HttpMethod.Post, "v1/matchperson")
        {
            Content = JsonContent.Create(
                new
                {
                    personSpecification = new
                    {
                        given = "Octavia",
                        family = "Chislett",
                        birthDate = "2008-09-20",
                        gender = "female",
                        phone = (string?)null,
                        email = (string?)null,
                        addressPostalCode = "KT19 0ST",
                    },
                }
            ),
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        request.Headers.Add("x-api-key", Fixture.Config.FindApiKey);

        var response = await Fixture.Client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = JsonSerializer.Deserialize<JsonElement>(responseBody);
        var hasPersonId =
            json.TryGetProperty("PersonId", out var personId)
            || json.TryGetProperty("personId", out personId);

        Assert.True(hasPersonId, "MatchPerson response did not contain PersonId.");
        Assert.False(string.IsNullOrWhiteSpace(personId.GetString()));
    }
}
