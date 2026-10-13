using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using NSubstitute;
using SUI.GetAnIdentifier.API.Middleware;
using SUI.GetAnIdentifier.API.Models;
using SUI.GetAnIdentifier.Infrastructure.Models;

namespace SUI.GetAnIdentifier.API.UnitTests.Middleware;

public class AuthContextFactoryTests
{
    private const string ClientId = "CLIENT_ID_EDUCATION_01";
    private const string OrganisationId = "EDUCATION-01";
    private readonly AuthContextFactory _sut;

    public AuthContextFactoryTests()
    {
        _sut = new AuthContextFactory();
    }

    [Fact]
    public void TestFromJwt_WhenClientIdAzpAndSubEmpty_ShouldReturnFailure()
    {
        // Arrange
        var claims = new List<Claim> { new("client_id", ""), new("azp", ""), new("sub", "") };
        var jwt = new JwtSecurityToken(claims: claims);

        // Act
        var result = _sut.FromJwt(jwt);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(AuthFailureReason.InvalidTokenClaims, result.FailureType);
        Assert.Equal("Token did not contain client_id, azp, or sub.", result.ErrorMessage);
        Assert.Null(result.Context);
    }

    [Fact]
    public void TestFromJwt_WhenNoClientIdAzpOrSub_ShouldReturnFailure()
    {
        // Arrange
        var jwt = new JwtSecurityToken();

        // Act
        var result = _sut.FromJwt(jwt);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(AuthFailureReason.InvalidTokenClaims, result.FailureType);
        Assert.Equal("Token did not contain client_id, azp, or sub.", result.ErrorMessage);
    }

    [Fact]
    public void TestFromJwt_WhenAzpAndSubPresent_UsesAzpAsClientId()
    {
        // Arrange
        var claims = new List<Claim> { new("azp", "AZP-CLIENT-01"), new("sub", "SUB-CLIENT-01") };
        var jwt = new JwtSecurityToken(claims: claims);

        var validClient = new AuthClient
        {
            Enabled = true,
            OrganisationId = "ORG-01",
            AllowedScopes = ["test.scope.azp"],
        };

        // Act
        var result = _sut.FromJwt(jwt);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("AZP-CLIENT-01", result.Context!.ClientId);
    }

    [Fact]
    public void TestFromJwt_WhenClientIdAndAzpPresent_UsesClientId()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("client_id", "MAIN-CLIENT-01"),
            new("azp", "AZP-CLIENT-01"),
            new("sub", "SUB-CLIENT-01"),
        };
        var jwt = new JwtSecurityToken(claims: claims);

        var validClient = new AuthClient
        {
            Enabled = true,
            OrganisationId = "ORG-01",
            AllowedScopes = ["test.scope.main"],
        };

        // Act
        var result = _sut.FromJwt(jwt);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("MAIN-CLIENT-01", result.Context!.ClientId);
    }

    [Fact]
    public void TestFromJwt_WhenOnlySubPresent_UsesSubAsClientId()
    {
        // Arrange
        var claims = new List<Claim> { new("sub", "SUB-CLIENT-01") };
        var jwt = new JwtSecurityToken(claims: claims);

        // Act
        var result = _sut.FromJwt(jwt);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("SUB-CLIENT-01", result.Context!.ClientId);
    }

    [Fact]
    public void TestFromJwt_WithValidInputs_UsingTokenScopes_ReturnsAuthContext()
    {
        // Arrange
        List<string> scopesList = ["file.read", "file.write"];

        var claims = new List<Claim>
        {
            new("client_id", ClientId),
            new("scope", "file.read file.write"),
        };
        var jwt = new JwtSecurityToken(claims: claims);

        // Act
        var result = _sut.FromJwt(jwt);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ClientId, result.Context!.ClientId);
        // Assert.Equal(scopesList, result.Context.Scopes);
    }
}
