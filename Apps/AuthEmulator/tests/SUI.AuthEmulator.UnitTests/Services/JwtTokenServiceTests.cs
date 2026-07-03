using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using SUI.AuthEmulator.Configurations;
using SUI.AuthEmulator.Models;
using SUI.AuthEmulator.Services;

namespace SUI.AuthEmulator.UnitTests.Services;

public class JwtTokenServiceTests
{
    private static (JwtTokenService Service, TimeProvider TimeProvider) CreateTestService()
    {
        var authSettings = new AuthSettings
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            TokenLifetimeMinutes = 60,
        };
        var mockOptions = Substitute.For<IOptions<AuthSettings>>();
        mockOptions.Value.Returns(authSettings);

        // Set up a mock RSA security key details
        var mockRsaKey = new RsaSecurityKey(System.Security.Cryptography.RSA.Create(2048))
        {
            KeyId = "test-kid",
        };
        var mockCredentials = new SigningCredentials(mockRsaKey, SecurityAlgorithms.RsaSha256);
        var mockKeyDetails = new RsaKeyDetails(mockRsaKey, mockCredentials, "modulus", "exponent");

        var mockJwksKeyProvider = Substitute.For<IJwksKeyProvider>();
        mockJwksKeyProvider.GetKeys().Returns(new List<RsaKeyDetails> { mockKeyDetails });

        var testTimeProvider = TimeProvider.System;

        var service = new JwtTokenService(mockOptions, mockJwksKeyProvider, testTimeProvider);

        return (service, testTimeProvider);
    }

    [Fact]
    public void GenerateToken_ReturnsTokenString()
    {
        // Arrange
        var (service, _) = CreateTestService();

        // Act
        var token = service.GenerateToken(
            "test-client-id",
            new List<string> { "scope1", "scope2" }
        );

        // Assert
        Assert.False(string.IsNullOrEmpty(token));

        // Validate that the generated token can be read and matches our config
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal("test-issuer", jwtToken.Issuer);
        Assert.Contains("test-audience", jwtToken.Audiences);
        Assert.Equal("test-kid", jwtToken.Header.Kid);
    }

    [Fact]
    public void GenerateToken_WhenNoKeysAvailable_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockOptions = Substitute.For<IOptions<AuthSettings>>();
        mockOptions.Value.Returns(new AuthSettings());

        var mockJwksKeyProvider = Substitute.For<IJwksKeyProvider>();
        // Return an empty list to simulate the provider failing to load keys
        mockJwksKeyProvider.GetKeys().Returns(new List<RsaKeyDetails>());

        var service = new JwtTokenService(mockOptions, mockJwksKeyProvider, TimeProvider.System);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.GenerateToken("test-client-id", new List<string>())
        );

        Assert.Equal(
            "No RSA signing keys are available from the JWKS provider.",
            exception.Message
        );
    }

    [Fact]
    public void GenerateToken_WithModeNotYetActive_SetsNotBeforeToFuture()
    {
        var (service, timeProvider) = CreateTestService();

        var token = service.GenerateToken("client", [], "not-yet-active");

        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);

        // Assert that the token doesn't become valid until at least 1 hour from now
        Assert.True(jwtToken.ValidFrom > timeProvider.GetUtcNow().UtcDateTime.AddHours(1));
    }

    [Fact]
    public void GenerateToken_WithModeExpired_SetsValidToPast()
    {
        var (service, timeProvider) = CreateTestService();

        var token = service.GenerateToken("client", [], "expired");

        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);

        // Assert that the token expired at least 1 hour ago
        Assert.True(jwtToken.ValidTo < timeProvider.GetUtcNow().UtcDateTime.AddHours(-1));
    }

    [Fact]
    public void GenerateToken_WithModeSpoofIssuer_SetsSpoofIssuer()
    {
        var (service, _) = CreateTestService();

        var token = service.GenerateToken("client", [], "spoof-issuer");

        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("spoof", jwtToken.Issuer);
    }

    [Fact]
    public void GenerateToken_WithModeSpoofAudience_SetsSpoofAudience()
    {
        var (service, _) = CreateTestService();

        var token = service.GenerateToken("client", [], "spoof-audience");

        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Contains("spoof", jwtToken.Audiences);
    }

    [Fact]
    public void GenerateToken_WithModeSpoofPrivateKey_UsesDifferentKeyId()
    {
        var (service, _) = CreateTestService();

        var token = service.GenerateToken("client", [], "spoof-private-key");

        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);

        // The mock sets the standard Kid to "test-kid". The spoofed one should generate a random Guid.
        Assert.NotEqual("test-kid", jwtToken.Header.Kid);
    }
}
