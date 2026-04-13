using System.Net;
using System.Text;
using System.Text.Json;
using SUI.StubCustodians.Application.Models;
using SUI.StubCustodians.Application.Utilities;

namespace SUI.StubCustodians.Application.Unit.Tests.Utilities;

public class TokenProviderTests
{
    [Fact]
    public async Task GetTokenAsync_ShouldFetchToken_FromApi()
    {
        var handler = new FakeHandler(req =>
        {
            var response = new AuthTokenResponse
            {
                AccessToken = "token-123",
                ExpiresIn = 3600,
                TokenType = "Bearer",
                Scope = "work-item.write",
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(response),
                    Encoding.UTF8,
                    "application/json"
                ),
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://find.test") };

        var provider = new TokenProvider(client);

        var token = await provider.GetTokenAsync("client", "secret");

        Assert.Equal("token-123", token);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldUseCache_WhenTokenValid()
    {
        var handler = new FakeHandler(req =>
        {
            var response = new AuthTokenResponse
            {
                AccessToken = "cached-token",
                ExpiresIn = 3600,
                TokenType = "Bearer",
                Scope = "work-item.write",
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(response)),
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://find.test") };

        var provider = new TokenProvider(client);

        var first = await provider.GetTokenAsync("client", "secret");
        var second = await provider.GetTokenAsync("client", "secret");

        Assert.Equal("cached-token", first);
        Assert.Equal("cached-token", second);

        Assert.Equal(1, handler.CallCount); // only one HTTP call
    }

    [Fact]
    public async Task GetTokenAsync_ShouldCachePerCredentialPair()
    {
        var handler = new FakeHandler(req =>
        {
            var response = new AuthTokenResponse
            {
                AccessToken = Guid.NewGuid().ToString(),
                ExpiresIn = 3600,
                TokenType = "Bearer",
                Scope = "work-item.write",
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(response)),
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://find.test") };

        var provider = new TokenProvider(client);

        var token1 = await provider.GetTokenAsync("client1", "secret1");
        var token2 = await provider.GetTokenAsync("client2", "secret2");

        Assert.NotEqual(token1, token2);

        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldThrow_WhenHttpFails()
    {
        var handler = new FakeHandler(req =>
        {
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://find.test") };

        var provider = new TokenProvider(client);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            provider.GetTokenAsync("client", "secret")
        );
    }
}
