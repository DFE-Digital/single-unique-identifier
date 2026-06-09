using NSubstitute;
using SUI.AuthEmulator.Models;
using SUI.AuthEmulator.Services;

namespace SUI.AuthEmulator.UnitTests.Services;

public class JwtTokenServiceTests
{
    [Fact]
    public async Task GenerateToken_ReturnsTokenString()
    {
        // Arrange
        var mockAuthStore = Substitute.For<IAuthStoreService>();
        mockAuthStore
            .GetAuthStoreAsync()
            .Returns(
                new AuthStore
                {
                    Issuer = "test-issuer",
                    Audience = "test-audience",
                    SigningKey = "test-signing-key-really-long-and-secure",
                    DefaultTokenLifetimeMinutes = 60,
                }
            );

        var service = new JwtTokenService(mockAuthStore);

        // Act
        var token = await service.GenerateToken(
            "test-client-id",
            new List<string> { "scope1", "scope2" }
        );

        // Assert
        Assert.False(string.IsNullOrEmpty(token));
    }
}
