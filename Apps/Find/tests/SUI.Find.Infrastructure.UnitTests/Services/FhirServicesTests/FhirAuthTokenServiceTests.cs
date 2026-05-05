using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using SUI.Find.Infrastructure.Models.Fhir;
using SUI.Find.Infrastructure.Services.Fhir;
using SUI.Find.Infrastructure.UnitTests.Utility;

namespace SUI.Find.Infrastructure.UnitTests.Services.FhirServicesTests;

public class FhirAuthTokenServiceTests
{
    private readonly IOptions<AuthTokenServiceConfig> _subOptions;
    private readonly ILogger<FhirAuthTokenService> _subLogger;
    private readonly IHttpClientFactory _subHttpClientFactory;
    private readonly MockHttpMessageHandler _mockHttpMessageHandler;

    private const string DummyToken = "a.dummy.token";
    private const string DummyClientId = "test-client-id";
    private const string DummyKid = "test-kid";

    public FhirAuthTokenServiceTests()
    {
        _subOptions = Substitute.For<IOptions<AuthTokenServiceConfig>>();
        _subLogger = Substitute.For<ILogger<FhirAuthTokenService>>();
        _subHttpClientFactory = Substitute.For<IHttpClientFactory>();

        _subOptions.Value.Returns(
            new AuthTokenServiceConfig
            {
                NHS_DIGITAL_ACCESS_TOKEN_EXPIRES_IN_MINUTES = 5,
                NHS_DIGITAL_PRIVATE_KEY = GenerateDummyPrivateKey(),
                NHS_DIGITAL_KID = DummyKid,
                NHS_DIGITAL_CLIENT_ID = DummyClientId,
            }
        );

        _mockHttpMessageHandler = new MockHttpMessageHandler();

        var httpClient = new HttpClient(_mockHttpMessageHandler)
        {
            BaseAddress = new Uri("https://test.auth.api/"),
        };

        _subHttpClientFactory.CreateClient("nhs-auth-api").Returns(httpClient);
    }

    private static string GenerateDummyPrivateKey()
    {
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        return Convert.ToBase64String(rsa.ExportRSAPrivateKey());
    }

    private FhirAuthTokenService CreateService()
    {
        return new FhirAuthTokenService(_subOptions, _subLogger, _subHttpClientFactory);
    }

    [Fact]
    public async Task GetBearerToken_FirstCall_InitializesAndFetchesNewToken()
    {
        // Arrange
        var service = CreateService();

        // Act
        var token = await service.GetBearerToken(CancellationToken.None);

        // Assert
        Assert.Equal(DummyToken, token);
        Assert.Equal(1, _mockHttpMessageHandler.NumberOfCalls);
    }

    [Fact]
    public async Task GetBearerToken_SecondCall_ReturnsCachedToken()
    {
        // Arrange
        var service = CreateService();
        await service.GetBearerToken(CancellationToken.None);

        // Act
        var token = await service.GetBearerToken(CancellationToken.None);

        // Assert
        Assert.Equal(DummyToken, token);
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
        await Task.Delay(1100); // Wait for token to expire
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
        for (var i = 0; i < taskCount; i++)
        {
            tasks.Add(service.GetBearerToken(CancellationToken.None));
        }
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, token => Assert.Equal(DummyToken, token));
        Assert.Equal(1, _mockHttpMessageHandler.NumberOfCalls);
    }

    [Fact]
    public async Task GetBearerToken_TokenEndpointFails_ThrowsHttpRequestException()
    {
        // Arrange
        var mockHttpErrorHandler = new MockHttpMessageHandler
        {
            StatusCode = HttpStatusCode.Unauthorized,
        };

        var errorHttpClient = new HttpClient(mockHttpErrorHandler)
        {
            BaseAddress = new Uri("https://test.auth.api/"),
        };
        _subHttpClientFactory.CreateClient("nhs-auth-api").Returns(errorHttpClient);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.GetBearerToken(CancellationToken.None)
        );
    }
}
