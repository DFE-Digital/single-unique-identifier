using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSubstitute;
using SUI.AuthEmulator.Configurations;
using SUI.AuthEmulator.Controllers;

namespace SUI.AuthEmulator.UnitTests.Controllers;

public class OIDCDiscoveryControllerTests
{
    private readonly IOptions<AuthSettings> _mockOptions = Substitute.For<IOptions<AuthSettings>>();

    [Fact]
    public void GetDiscoveryDocument_ShouldReturnOkWithMappedProperties()
    {
        // Arrange
        var settings = new AuthSettings
        {
            Issuer = "https://test.issuer.com",
            BaseUrl = "https://test.api.com/", // Testing with a trailing slash to verify truncation logic
        };
        _mockOptions.Value.Returns(settings);

        var sut = new OIDCDiscoveryController(_mockOptions);

        // Act
        var response = sut.GetDiscoveryDocument();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        Assert.NotNull(okResult.Value);

        // Extract anonymous properties safely using reflection
        var val = okResult.Value;
        Assert.Equal("https://test.issuer.com", val.GetType().GetProperty("issuer")?.GetValue(val));
        Assert.Equal(
            "https://test.api.com/api/v1/auth/token",
            val.GetType().GetProperty("token_endpoint")?.GetValue(val)
        );
        Assert.Equal(
            "https://test.api.com/api/v1/jwks",
            val.GetType().GetProperty("jwks_uri")?.GetValue(val)
        );
        Assert.Equal(
            "https://test.api.com/dummy",
            val.GetType().GetProperty("authorization_endpoint")?.GetValue(val)
        );

        var algs =
            val.GetType().GetProperty("id_token_signing_alg_values_supported")?.GetValue(val)
            as string[];
        Assert.Contains("RS256", algs!);
    }
}
