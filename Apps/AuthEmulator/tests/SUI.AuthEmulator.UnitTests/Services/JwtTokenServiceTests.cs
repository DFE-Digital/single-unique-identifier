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
    [Fact]
    public void GenerateToken_ReturnsTokenString()
    {
        // Arrange
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
}
