using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SUI.Find.E2ETests;

public class E2ETestBase
{
    protected readonly FunctionTestFixture Fixture;

    protected E2ETestBase(FunctionTestFixture fixture)
    {
        Fixture = fixture;
    }

    protected async Task<string?> GetAuthTokenAsync(
        string clientId,
        string clientSecret,
        string[] scopes
    )
    {
        var formData = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "scope", string.Join(" ", scopes) },
        };

        var content = new FormUrlEncodedContent(formData);

        var request = new HttpRequestMessage(HttpMethod.Post, "v1/auth/token")
        {
            Content = content,
        };

        var authString = $"{clientId}:{clientSecret}";
        var base64Auth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(authString));

        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64Auth);

        var response = await Fixture.Client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Auth Failed: {response.StatusCode} - {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        return result.GetProperty("access_token").GetString();
    }
}
