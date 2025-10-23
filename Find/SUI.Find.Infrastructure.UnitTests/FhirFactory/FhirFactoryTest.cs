using NSubstitute;
using Microsoft.Extensions.Options;
using SUi.Find.Application.Interfaces;
using SUI.Find.Infrastructure.Models;
using SUI.Find.Infrastructure.Fhir;

namespace SUI.Find.Infrastructure.UnitTests.FhirFactory;

public class FhirClientFactoryTest
{
    private readonly IAuthTokenService _mockAuthTokenService;
    private readonly FhirClientFactory _factory;

    public FhirClientFactoryTest()
    {
        _mockAuthTokenService = Substitute.For<IAuthTokenService>();

        var config = new AuthTokenServiceConfig
        {
            NHS_DIGITAL_FHIR_ENDPOINT = "https://example.com/fhir/"
        };
        var mockOptions = Options.Create(config);
        _factory = new FhirClientFactory(_mockAuthTokenService, mockOptions);
    }

    [Fact]
    public void CreateFhirClient_ShouldReturnConfiguredClient()
    {
        // Arr
        const string fakeToken = "fake-bearer-token-123";

        _mockAuthTokenService.GetBearerToken(CancellationToken.None)
            .Returns(Task.FromResult(fakeToken));

        // Act
        var fhirClient = _factory.CreateFhirClient();

        // Assert
        Assert.Equal("https://example.com/fhir/", fhirClient.Endpoint.ToString());
        Assert.NotNull(fhirClient.RequestHeaders);
        Assert.NotNull(fhirClient.RequestHeaders.Authorization);
        Assert.Equal("Bearer", fhirClient.RequestHeaders.Authorization.Scheme);
        Assert.Equal(fakeToken, fhirClient.RequestHeaders.Authorization.Parameter);
        Assert.True(fhirClient.RequestHeaders.Contains("X-Request-ID"));
        _mockAuthTokenService.Received(1).GetBearerToken(CancellationToken.None);
    }
}