using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using SUI.Find.Infrastructure.Constants;
using System.Net;
using NSubstitute.ExceptionExtensions;
using SUi.Find.Application.Interfaces;
using SUI.Find.Infrastructure.Models;
using SUI.Find.Infrastructure.Services;
using SUI.Find.Infrastructure.UnitTests.TestUtils;

namespace SUI.Find.Infrastructure.UnitTests.Services;

public class AuthTokenServiceTests
{
    private readonly IOptions<AuthTokenServiceConfig> _subOptions;
    private readonly ILogger<AuthTokenService> _subLogger;
    private readonly IHttpClientFactory _subHttpClientFactory;
    private readonly ISecretService _subSecretService;
    private readonly MockHttpMessageHandler _mockHttpMessageHandler;

    private const string DummyToken = "a.dummy.token";

    private const string DummyPrivateKey = """
                                           -----BEGIN RSA PRIVATE KEY-----
                                           MIICWgIBAAKBgFO1fY49w+i7dyUui3gd2lzHtTh/5uZn98Ai3DyigxVBzE1SdMsh
                                           yt6xRIa6/gwpTrQGd3yxx51ud8l675fu5i10IJ7BjxKUHF0EBMidR4I9AbqRRjmk
                                           FNQnP4n6B0coQXldD2WSXaS2U1en8L4sGQUAAT0pRUEg8T22oCE8wKDTAgMBAAEC
                                           gYAFjGoeG4n4yzRCiqtD8vaeX75rWE79xrZtTeI7QqpdplbcaTLEpCDGUgmwxIRC
                                           WhqVZDhXU5FfpgramAN5lqQ7G4S+61+gdMHvtY+oCFxd7DfghbRri7LQOP3ums4E
                                           uhy1/5ohUjesIk1MKMCHU7tKDeyKKctT/cqd4DLYENx1wQJBAKS5iYC+GE+8xbuJ
                                           PvidtLI0ZOxmshDrCdl94YeIJfvFQy9iy3EZ038CN00f/1to2weSVdTfHtX7+GYf
                                           2u/k/k8CQQCCF7wkJKu/SDn29VCYnh4gYgjwfHNLmUtCFC+2HRg1W6y7GmWKz+++
                                           ikNbe2hJsqbVoN9pwyQc1mj6p2PIlXg9AkBYUCCoJUJjfZGFOc/I+sQlxnFVTLmq
                                           2Fgvgo2nXBcBJIEgppbrzCzXqxh7AOym1VCYfpwFxJmDn9NM7Ucz1lGBAkAvXNzO
                                           e9tbhLw1wRJavhZRy99dTrHbMDBKGndUYjtSEdJNPEsDwriSMlxbjg5l5nj/BdbQ
                                           9o7LQPRvbUnS2TgxAkALVbOWrBW0NUg6PS2kACaX5nMvGU+qH4Zmk3atBKhL2PDl
                                           V8n23abMyu2iFczaQvORFmZjsirX2bN9/BKXVc7d
                                           -----END RSA PRIVATE KEY-----
                                           """;
    private const string DummyClientId = "test-client-id";
    private const string DummyKid = "test-kid";

    public AuthTokenServiceTests()
    {
        _subOptions = Substitute.For<IOptions<AuthTokenServiceConfig>>();
        _subLogger = Substitute.For<ILogger<AuthTokenService>>();
        _subHttpClientFactory = Substitute.For<IHttpClientFactory>();
        _subSecretService = Substitute.For<ISecretService>();

        _subOptions.Value.Returns(new AuthTokenServiceConfig
        {
            NHS_DIGITAL_ACCESS_TOKEN_EXPIRES_IN_MINUTES = 5
        });

        _mockHttpMessageHandler = new MockHttpMessageHandler();

        var httpClient = new HttpClient(_mockHttpMessageHandler)
        {
            BaseAddress = new Uri("https://test.auth.api/")
        };

        _subHttpClientFactory.CreateClient("nhs-auth-api").Returns(httpClient);

        SetupSecret(NhsDigitalKeyConstants.PrivateKey, DummyPrivateKey);
        SetupSecret(NhsDigitalKeyConstants.ClientId, DummyClientId);
        SetupSecret(NhsDigitalKeyConstants.Kid, DummyKid);
    }

    private void SetupSecret(string secretName, string secretValue)
    {
        _subSecretService.GetSecret(secretName, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(secretValue));
    }

    private AuthTokenService CreateService()
    {
        return new AuthTokenService(
            _subOptions,
            _subLogger,
            _subHttpClientFactory,
            _subSecretService
        );
    }

    [Fact]
    public async Task GetBearerToken_FirstCall_InitializesAndFetchesNewToken()
    {
        // Arrange
        var service = CreateService();

        // Act
        var token = await service.GetBearerToken(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(DummyToken, token);
        await _subSecretService.Received(1).GetSecret(NhsDigitalKeyConstants.PrivateKey, Arg.Any<CancellationToken>());
        await _subSecretService.Received(1).GetSecret(NhsDigitalKeyConstants.ClientId, Arg.Any<CancellationToken>());
        await _subSecretService.Received(1).GetSecret(NhsDigitalKeyConstants.Kid, Arg.Any<CancellationToken>());
        Assert.Equal(1, _mockHttpMessageHandler.NumberOfCalls);
    }

    [Fact]
    public async Task GetBearerToken_SecondCall_ReturnsCachedToken()
    {
        // Arrange
        var service = CreateService();
        await service.GetBearerToken(TestContext.Current.CancellationToken);

        // Act
        var token = await service.GetBearerToken(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(DummyToken, token);
        await _subSecretService.Received(1).GetSecret(NhsDigitalKeyConstants.PrivateKey, Arg.Any<CancellationToken>());
        await _subSecretService.Received(1).GetSecret(NhsDigitalKeyConstants.ClientId, Arg.Any<CancellationToken>());
        await _subSecretService.Received(1).GetSecret(NhsDigitalKeyConstants.Kid, Arg.Any<CancellationToken>());
        Assert.Equal(1, _mockHttpMessageHandler.NumberOfCalls);
    }

    [Fact]
    public async Task GetBearerToken_WhenTokenIsExpired_FetchesNewToken()
    {
        // Arrange
        _mockHttpMessageHandler.ExpiresInSeconds = 1;
        var service = CreateService();

        // Act
        await service.GetBearerToken(CancellationToken.None);

        _mockHttpMessageHandler.ExpiresInSeconds = 300;

        var token = await service.GetBearerToken(CancellationToken.None);

        // Assert
        Assert.Equal(DummyToken, token);
        Assert.Equal(2, _mockHttpMessageHandler.NumberOfCalls);
    }

    [Fact]
    public async Task GetBearerToken_ConcurrentCalls_OnlyFetchesTokenOnce()
    {
        // Arrange
        var service = CreateService();
        const int taskCount = 100;
        var tasks = new List<Task<string>>();

        // Act
        for (var i = 0; i < taskCount; i++) tasks.Add(service.GetBearerToken(TestContext.Current.CancellationToken));
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, token => Assert.Equal(DummyToken, token));
        Assert.Equal(1, _mockHttpMessageHandler.NumberOfCalls);
        await _subSecretService.Received(1).GetSecret(NhsDigitalKeyConstants.PrivateKey, Arg.Any<CancellationToken>());
        await _subSecretService.Received(1).GetSecret(NhsDigitalKeyConstants.ClientId, Arg.Any<CancellationToken>());
        await _subSecretService.Received(1).GetSecret(NhsDigitalKeyConstants.Kid, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetBearerToken_SecretFetchFails_ThrowsInvalidOperationException()
    {
        // Arrange
        _subSecretService.GetSecret(NhsDigitalKeyConstants.PrivateKey, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Failed to get secret: "));

        var service = CreateService();

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.GetBearerToken(TestContext.Current.CancellationToken));
        Assert.Contains("Failed to get secret", exception.Message);
    }

    [Fact]
    public async Task GetBearerToken_TokenEndpointFails_ThrowsHttpRequestException()
    {
        // Arrange
        var mockHttpErrorHandler = new MockHttpMessageHandler
        {
            StatusCode = HttpStatusCode.Unauthorized
        };

        var errorHttpClient = new HttpClient(mockHttpErrorHandler) { BaseAddress = new Uri("https://test.auth.api/") };
        _subHttpClientFactory.CreateClient("nhs-auth-api").Returns(errorHttpClient);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.GetBearerToken(TestContext.Current.CancellationToken));
    }
}