using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using SUI.Find.Application.Configurations;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Services;
using SUI.Find.Domain.Models;
using SUI.Find.Infrastructure.Services;
using SUI.Find.Infrastructure.Utility;

namespace SUI.Find.E2ETests;

[Trait("Category", "E2E")]
[Trait("Suite", "Standard")]
public sealed class TestOutboundAuthService(
    FunctionTestFixture fixture,
    ITestOutputHelper testOutputHelper
) : IAsyncLifetime
{
    /// <summary>
    /// Runs before each individual test
    /// </summary>
    public async ValueTask InitializeAsync() =>
        await fixture.EnsureServicesAreUpAsync(testOutputHelper);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task TestOutboundAuth_RealService_ReturnsToken()
    {
        // Arrange
        var logger = Substitute.For<ILogger<OutboundAuthService>>();
        using var httpClient = new HttpClient();
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory
            .CreateClient(ApplicationConstants.Providers.LoggingName)
            .Returns(httpClient);

        var authService = new OutboundAuthService(logger, httpClientFactory);

        var providerDefinition = new ProviderDefinition
        {
            ProviderName = "Local-Authority-01",
            Connection = new ConnectionDefinition
            {
                Auth = new AuthDefinition
                {
                    TokenUrl = $"{fixture.StubCustodiansClient.BaseAddress}v1/auth/token",
                    ClientId = "SUI-SERVICE",
                    ClientSecret = "SUIProject",
                    Scopes = new List<string> { "find-record.read", "fetch-record.read" },
                },
            },
        };

        // Act
        var result = await authService.GetAccessTokenAsync(
            providerDefinition,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.Success, $"Failed to get token: {result.Error}");
        Assert.False(string.IsNullOrWhiteSpace(result.Value), "Token should not be empty");
    }

    [Fact]
    public async Task TestQueryProvidersService_RealService_ReturnsManifest()
    {
        var loggerQueryProviders = Substitute.For<ILogger<QueryProvidersService>>();
        var loggerBuildRequest = Substitute.For<ILogger<BuildCustodianRequestsService>>();
        var loggerOutboundAuth = Substitute.For<ILogger<OutboundAuthService>>();
        var loggerProviderHttp = Substitute.For<ILogger<ProviderHttpClient>>();
        var loggerEncryption = Substitute.For<ILogger<PersonIdEncryptionService>>();
        var loggerMaskUrl = Substitute.For<ILogger<MaskUrlService>>();
        var encryptionConfig = Substitute.For<IOptions<EncryptionConfiguration>>();

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var httpClientFactory2 = Substitute.For<IHttpClientFactory>();
        httpClientFactory
            .CreateClient(ApplicationConstants.Providers.LoggingName)
            .Returns(new HttpClient());
        httpClientFactory2
            .CreateClient(ApplicationConstants.Providers.LoggingName)
            .Returns(new HttpClient());

        var outboundAuthService = new OutboundAuthService(loggerOutboundAuth, httpClientFactory);
        var providerHttpClient = new ProviderHttpClient(httpClientFactory2, loggerProviderHttp);
        var encryptionService = new PersonIdEncryptionService(loggerEncryption);
        var requestBuilder = new BuildCustodianHttpRequest();
        encryptionConfig.Value.Returns(
            new EncryptionConfiguration { EnablePersonIdEncryption = false }
        );

        var buildCustodianRequestService = new BuildCustodianRequestsService(
            loggerBuildRequest,
            requestBuilder,
            providerHttpClient,
            outboundAuthService,
            encryptionService,
            encryptionConfig
        );

        var fetchUrlStorageService = Substitute.For<IFetchUrlStorageService>();
        var maskUrlService = new MaskUrlService(loggerMaskUrl, fetchUrlStorageService);

        var queryProvidersService = new QueryProvidersService(
            buildCustodianRequestService,
            loggerQueryProviders,
            maskUrlService
        );

        var providerDefinition = new ProviderDefinition
        {
            OrgId = "LOCAL-AUTHORITY-01",
            ProviderName = "Local-Authority-01",
            Encryption = new EncryptionDefinition
            {
                KeyId = "LOCAL-AUTHORITY-01-KEY-1",
                Algorithm = "AES-256-CBC",
                Key = "Kz4KY01cCpp1rvQ4Lj540n8Pp4EHBZ0HbNm6KvuzUaw=",
            },
            Connection = new ConnectionDefinition
            {
                Url =
                    $"{fixture.StubCustodiansClient.BaseAddress}v1/find/local-authority-01/{{personId}}?recordType=childrens-services.details",
                Method = "GET",
                PersonIdPosition = "path",
                Auth = new AuthDefinition
                {
                    TokenUrl = $"{fixture.StubCustodiansClient.BaseAddress}v1/auth/token",
                    ClientId = "SUI-SERVICE",
                    ClientSecret = "SUIProject",
                    Scopes = new List<string> { "find-record.read", "fetch-record.read" },
                },
            },
        };
        var data = new QueryProviderInput(
            "local-authority-01",
            "1234",
            "1234567890",
            "9691292211",
            providerDefinition
        );

        var results = await queryProvidersService.QueryProvidersAsync(data, CancellationToken.None);

        Assert.True(results.Success, $"Failed to query providers: {results.Error}");
        Assert.NotNull(results.Value);
        var item = results.Value[0];
        Assert.NotStrictEqual("local-authority-01", item.CustodianId);
        Assert.Equal("childrens-services.details", item.RecordType);
    }
}
