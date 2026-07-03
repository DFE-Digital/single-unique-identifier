using System.Net;
using System.Net.Http.Headers;

namespace SUI.Find.E2ETests;

[Trait("Category", "E2E")]
[Trait("Suite", "Standard")]
public class MatchAuthE2ETests(FunctionTestFixture fixture, ITestOutputHelper testOutputHelper)
    : E2ETestBase(fixture, testOutputHelper),
        IAsyncLifetime
{
    private const string MatchEndpointUrl = "v1/matchperson";
    private const string ValidClientId = "CLIENT_ID_LOCAL_AUTHORITY_01";
    private const string InvalidScopeClientId = "CLIENT_ID_INVALID_SCOPE";
    private const string TestClientSecret = "SUIProject";

    public async ValueTask InitializeAsync()
    {
        await Fixture.EnsureFindApiIsUpAsync(TestOutputHelper);
        await Fixture.EnsureAuthEmulatorApiIsUpAsync(TestOutputHelper);
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    private async Task<HttpResponseMessage> CallMatchApiAsync(string token, string body = "{}")
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, MatchEndpointUrl);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var apiKey =
            Fixture.Configuration["MatchFunctionConfiguration:XApiKey"] ?? "local-dev-api-key";
        request.Headers.Add("x-api-key", apiKey);

        request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        return await Fixture.Client.SendAsync(request);
    }

    private async Task AssertProblemDetailsAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedErrorIndicator
    )
    {
        Assert.Equal(expectedStatus, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        if (
            string.IsNullOrWhiteSpace(content)
            && response.StatusCode == HttpStatusCode.Unauthorized
        )
        {
            var authHeader = response.Headers.WwwAuthenticate.ToString();
            Assert.Contains(
                expectedErrorIndicator.ToLowerInvariant(),
                authHeader.ToLowerInvariant()
            );
            return;
        }

        Assert.False(
            string.IsNullOrWhiteSpace(content),
            "Expected a response body or WWW-Authenticate header, but got empty."
        );
        Assert.Contains(expectedErrorIndicator.ToLowerInvariant(), content.ToLowerInvariant());
    }

    [Fact]
    public async Task ValidToken_ShouldAllowAccess()
    {
        var token = await Fixture.AccessTokenProvider.GetAuthTokenAsync(
            ValidClientId,
            TestClientSecret,
            null,
            TestOutputHelper
        );
        var response = await CallMatchApiAsync(token!);

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.BadRequest,
            "personspecification is required"
        );
    }

    [Fact]
    public async Task ValidToken_MissingRequiredScope_ShouldDenyAccess()
    {
        var token = await Fixture.AccessTokenProvider.GetAuthTokenAsync(
            InvalidScopeClientId,
            TestClientSecret,
            null,
            TestOutputHelper
        );
        var response = await CallMatchApiAsync(token!);
        await AssertProblemDetailsAsync(response, HttpStatusCode.Forbidden, "insufficient scope");
    }

    [Fact]
    public async Task InvalidSignature_ModifiedPayload_ShouldDenyAccess()
    {
        var token = await Fixture.AccessTokenProvider.GetAuthTokenAsync(
            ValidClientId,
            TestClientSecret,
            null,
            TestOutputHelper
        );
        var parts = token!.Split('.');

        var payload = parts[1];
        parts[1] = payload[..^1] + (payload.EndsWith('A') ? "B" : "A");
        var tamperedToken = string.Join('.', parts);

        var response = await CallMatchApiAsync(tamperedToken);
        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.Unauthorized,
            "token validation failed"
        );
    }

    [Fact]
    public async Task InvalidSignature_ModifiedHeader_ShouldDenyAccess()
    {
        var token = await Fixture.AccessTokenProvider.GetAuthTokenAsync(
            ValidClientId,
            TestClientSecret,
            null,
            TestOutputHelper
        );
        var parts = token!.Split('.');

        var header = parts[0];
        parts[0] = header[..^1] + (header.EndsWith('A') ? "B" : "A");
        var tamperedToken = string.Join('.', parts);

        var response = await CallMatchApiAsync(tamperedToken);
        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.Unauthorized,
            "token validation failed"
        );
    }

    [Fact]
    public async Task InvalidSignature_RemovedSignature_ShouldDenyAccess()
    {
        var token = await Fixture.AccessTokenProvider.GetAuthTokenAsync(
            ValidClientId,
            TestClientSecret,
            null,
            TestOutputHelper
        );
        var parts = token!.Split('.');
        var tamperedToken = $"{parts[0]}.{parts[1]}.";

        var response = await CallMatchApiAsync(tamperedToken);
        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.Unauthorized,
            "token validation failed"
        );
    }

    [Fact]
    [Trait("RequiresAuthEmulator", "true")]
    public async Task InvalidTime_NotYetActive_ShouldDenyAccess()
    {
        var token = await Fixture.AccessTokenProvider.GetAuthTokenAsync(
            ValidClientId,
            TestClientSecret,
            null,
            TestOutputHelper,
            mode: "not-yet-active"
        );
        var response = await CallMatchApiAsync(token!);
        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.Unauthorized,
            "token validation failed"
        );
    }

    [Fact]
    [Trait("RequiresAuthEmulator", "true")]
    public async Task InvalidTime_Expired_ShouldDenyAccess()
    {
        var token = await Fixture.AccessTokenProvider.GetAuthTokenAsync(
            ValidClientId,
            TestClientSecret,
            null,
            TestOutputHelper,
            mode: "expired"
        );
        var response = await CallMatchApiAsync(token!);
        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.Unauthorized,
            "token validation failed"
        );
    }

    [Fact]
    [Trait("RequiresAuthEmulator", "true")]
    public async Task InvalidSignature_SpoofPrivateKey_ShouldDenyAccess()
    {
        var token = await Fixture.AccessTokenProvider.GetAuthTokenAsync(
            ValidClientId,
            TestClientSecret,
            null,
            TestOutputHelper,
            mode: "spoof-private-key"
        );
        var response = await CallMatchApiAsync(token!);
        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.Unauthorized,
            "token validation failed"
        );
    }

    [Fact]
    [Trait("RequiresAuthEmulator", "true")]
    public async Task InvalidSource_SpoofIssuer_ShouldDenyAccess()
    {
        var token = await Fixture.AccessTokenProvider.GetAuthTokenAsync(
            ValidClientId,
            TestClientSecret,
            null,
            TestOutputHelper,
            mode: "spoof-issuer"
        );
        var response = await CallMatchApiAsync(token!);
        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.Unauthorized,
            "token validation failed"
        );
    }

    [Fact]
    [Trait("RequiresAuthEmulator", "true")]
    public async Task InvalidTarget_SpoofAudience_ShouldDenyAccess()
    {
        var token = await Fixture.AccessTokenProvider.GetAuthTokenAsync(
            ValidClientId,
            TestClientSecret,
            null,
            TestOutputHelper,
            mode: "spoof-audience"
        );
        var response = await CallMatchApiAsync(token!);
        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.Unauthorized,
            "token validation failed"
        );
    }
}
