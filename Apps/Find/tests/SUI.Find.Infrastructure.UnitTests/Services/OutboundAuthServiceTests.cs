using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Models;
using SUI.Find.Infrastructure.Models;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.Infrastructure.UnitTests.Services;

public class OutboundAuthServiceTests
{
    private readonly ILogger<OutboundAuthService> _logger = Substitute.For<
        ILogger<OutboundAuthService>
    >();
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();

    [Fact]
    public async Task Should_ReturnFail_When_AuthConfigIsNull()
    {
        var provider = new ProviderDefinition
        {
            ProviderName = "Test",
            Connection = new ConnectionDefinition { Auth = null },
        };
        var service = new OutboundAuthService(_logger, _httpClientFactory);

        var result = await service.GetAccessTokenAsync(provider, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("No auth configuration for provider.", result.Error);
    }

    [Fact]
    public async Task Should_ReturnFail_When_AuthConfigIsInvalid()
    {
        var provider = new ProviderDefinition
        {
            ProviderName = "Test",
            Connection = new ConnectionDefinition
            {
                Auth = new AuthDefinition
                {
                    TokenUrl = "",
                    ClientId = "",
                    ClientSecret = "",
                },
            },
        };
        var service = new OutboundAuthService(_logger, _httpClientFactory);

        var result = await service.GetAccessTokenAsync(provider, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Invalid auth configuration for provider.", result.Error);
    }

    [Fact]
    public async Task Should_ReturnFail_When_HttpResponseIsNotSuccess()
    {
        var provider = TestProvider();
        var handler = new HttpClient(
            new MockHttpMessageHandler(
                (req, ct) =>
                    Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.BadRequest)
                        {
                            Content = new StringContent(""),
                        }
                    )
            )
        );
        _httpClientFactory.CreateClient("providers").Returns(handler);

        var service = new OutboundAuthService(_logger, _httpClientFactory);

        var result = await service.GetAccessTokenAsync(provider, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Failed to obtain access token from provider.", result.Error);
    }

    [Fact]
    public async Task Should_ReturnFail_When_TokenResponseIsInvalid()
    {
        var provider = TestProvider();
        var handler = new HttpClient(
            new MockHttpMessageHandler(
                (req, ct) =>
                    Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.BadRequest)
                        {
                            Content = new StringContent(""),
                        }
                    )
            )
        );

        _httpClientFactory.CreateClient("providers").Returns(handler);

        var service = new OutboundAuthService(_logger, _httpClientFactory);

        var result = await service.GetAccessTokenAsync(provider, CancellationToken.None);

        Assert.False(result.Success);
        Assert.False(string.IsNullOrEmpty(result.Error));
    }

    [Fact]
    public async Task Should_ReturnAccessToken_When_ResponseIsValid()
    {
        var authTokenResponse = new OAuthTokenResponse
        {
            AccessToken = "token123",
            TokenType = "someType",
            ExpiresIn = 100,
        };
        var provider = TestProvider();
        var handler = new HttpClient(
            new MockHttpMessageHandler(
                (req, ct) =>
                    Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                JsonSerializer.Serialize(authTokenResponse)
                            ),
                        }
                    )
            )
        );
        _httpClientFactory.CreateClient("providers").Returns(handler);

        var service = new OutboundAuthService(_logger, _httpClientFactory);

        var result = await service.GetAccessTokenAsync(provider, CancellationToken.None);
        Assert.True(result.Success);
        Assert.Equal("token123", result.Value);
    }

    private static ProviderDefinition TestProvider() =>
        new ProviderDefinition
        {
            ProviderName = "Test",
            Connection = new ConnectionDefinition
            {
                Auth = new AuthDefinition
                {
                    TokenUrl = "https://token.url",
                    ClientId = "client",
                    ClientSecret = "secret",
                    Scopes = new List<string> { "scope1" },
                },
            },
        };

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<
            HttpRequestMessage,
            CancellationToken,
            Task<HttpResponseMessage>
        > _handlerFunc;

        public MockHttpMessageHandler(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc
        )
        {
            _handlerFunc = handlerFunc;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            return _handlerFunc(request, cancellationToken);
        }
    }
}
