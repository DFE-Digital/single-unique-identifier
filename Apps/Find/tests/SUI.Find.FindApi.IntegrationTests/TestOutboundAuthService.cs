using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Models;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.FindApi.IntegrationTests;

public class TestOutboundAuthService
{
    [Fact]
    public async Task TestOutboundAuth_RealService_ReturnsToken()
    {
        // Arrange
        var logger = Substitute.For<ILogger<OutboundAuthService>>();
        var httpClient = new HttpClient();
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
                    TokenUrl = "https://localhost:7256/api/v1/auth/token",
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
}
