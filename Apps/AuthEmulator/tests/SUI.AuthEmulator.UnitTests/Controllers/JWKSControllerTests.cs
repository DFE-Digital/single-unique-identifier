using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using SUI.AuthEmulator.Controllers;
using SUI.AuthEmulator.Models;
using SUI.AuthEmulator.Services;

namespace SUI.AuthEmulator.UnitTests.Controllers;

public class JWKSControllerTests
{
    private readonly IJwksKeyProvider _mockKeyProvider = Substitute.For<IJwksKeyProvider>();
    private readonly JWKSController _sut;

    public JWKSControllerTests()
    {
        _sut = new JWKSController(_mockKeyProvider);
    }

    [Fact]
    public void GetJwks_ShouldReturnOkWithCorrectJsonStructure()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var rsaSecurityKey = new RsaSecurityKey(rsa) { KeyId = "test-key-id" };
        var mockDetails = new RsaKeyDetails(
            rsaSecurityKey,
            new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256),
            "mock-modulus-n",
            "mock-exponent-e"
        );

        _mockKeyProvider.GetKeys().Returns(new[] { mockDetails });

        // Act
        var response = _sut.GetJwks();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        Assert.NotNull(okResult.Value);

        // Dig into the anonymous structure returned by the controller
        var keysProperty = okResult.Value.GetType().GetProperty("keys");
        Assert.NotNull(keysProperty);

        var keysEnumerable =
            keysProperty.GetValue(okResult.Value) as System.Collections.IEnumerable;
        Assert.NotNull(keysEnumerable);

        var keyList = keysEnumerable.Cast<object>().ToList();
        Assert.Single(keyList);

        // Verify the individual JWKS parameter mapping
        var firstKey = keyList.First();

        Assert.Equal("RSA", firstKey.GetType().GetProperty("kty")?.GetValue(firstKey));
        Assert.Equal("sig", firstKey.GetType().GetProperty("use")?.GetValue(firstKey));
        Assert.Equal("RS256", firstKey.GetType().GetProperty("alg")?.GetValue(firstKey));
        Assert.Equal("test-key-id", firstKey.GetType().GetProperty("kid")?.GetValue(firstKey));
        Assert.Equal("mock-modulus-n", firstKey.GetType().GetProperty("n")?.GetValue(firstKey));
        Assert.Equal("mock-exponent-e", firstKey.GetType().GetProperty("e")?.GetValue(firstKey));
    }
}
