using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using NSubstitute;
using SUI.Find.FindApi.Middleware;
using SUI.Find.FindApi.Models.Auth;
using SUI.Find.Infrastructure.Models;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.FindApi.UnitTests.MiddlewareTests;

public class AuthContextFactoryTests
{
    private const string ClientId = "CLIENT_ID_EDUCATION_01";
    private const string OrganisationId = "EDUCATION-01";
    private readonly AuthContextFactory _sut;
    private readonly IAuthStoreService _store;

    public AuthContextFactoryTests()
    {
        _store = Substitute.For<IAuthStoreService>();
        _sut = new AuthContextFactory(_store);
    }

    [Fact]
    public void TestFromJwt_WhenClientIdAzpAndSubEmpty_ShouldReturnFailure()
    {
        // Arrange
        var claims = new List<Claim> { new("client_id", ""), new("azp", ""), new("sub", "") };
        var jwt = new JwtSecurityToken(claims: claims);

        // Act
        var result = _sut.FromJwt(jwt, false);

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
        var result = _sut.FromJwt(jwt, false);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(AuthFailureReason.InvalidTokenClaims, result.FailureType);
        Assert.Equal("Token did not contain client_id, azp, or sub.", result.ErrorMessage);
    }

    [Fact]
    public void TestFromJwt_WhenClientNotFound_ShouldReturnFailure()
    {
        // Arrange
        var claims = new List<Claim> { new("client_id", ClientId) };
        var jwt = new JwtSecurityToken(claims: claims);
        _store.GetClientById(ClientId).Returns((AuthClient?)null);

        // Act
        var result = _sut.FromJwt(jwt, false);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(AuthFailureReason.ClientNotFound, result.FailureType);
        Assert.Equal("No matching client found in auth store.", result.ErrorMessage);
    }

    [Fact]
    public void TestFromJwt_WhenClientIsDisabled_ShouldReturnFailure()
    {
        // Arrange
        var claims = new List<Claim> { new("client_id", ClientId) };
        var jwt = new JwtSecurityToken(claims: claims);

        var disabledClient = new AuthClient
        {
            Enabled = false,
            OrganisationId = OrganisationId,
            AllowedScopes = ["test.scope.disabled"],
        };
        _store.GetClientById(ClientId).Returns(disabledClient);

        // Act
        var result = _sut.FromJwt(jwt, false);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(AuthFailureReason.ClientDisabled, result.FailureType);
        Assert.Equal("Client is disabled.", result.ErrorMessage);
    }

    [Fact]
    public void TestFromJwt_WhenNoOrganisationIdForClientId_ShouldReturnFailure()
    {
        // Arrange
        var claims = new List<Claim> { new("client_id", ClientId) };
        var jwt = new JwtSecurityToken(claims: claims);

        var invalidClient = new AuthClient
        {
            Enabled = true,
            OrganisationId = "",
            AllowedScopes = ["test.scope.missing-org"],
        };
        _store.GetClientById(ClientId).Returns(invalidClient);

        // Act
        var result = _sut.FromJwt(jwt, false);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(AuthFailureReason.MissingOrganisationId, result.FailureType);
        Assert.Equal("No Organisation ID found for client in auth store.", result.ErrorMessage);
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
        _store.GetClientById("AZP-CLIENT-01").Returns(validClient);

        // Act
        var result = _sut.FromJwt(jwt, false);

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
        _store.GetClientById("MAIN-CLIENT-01").Returns(validClient);

        // Act
        var result = _sut.FromJwt(jwt, false);

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

        var validClient = new AuthClient
        {
            Enabled = true,
            OrganisationId = "ORG-01",
            AllowedScopes = ["test.scope.sub"],
        };
        _store.GetClientById("SUB-CLIENT-01").Returns(validClient);

        // Act
        var result = _sut.FromJwt(jwt, false);

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

        var validClient = new AuthClient
        {
            Enabled = true,
            OrganisationId = OrganisationId,
            AllowedScopes = ["should.be.ignored"],
        };
        _store.GetClientById(ClientId).Returns(validClient);

        // Act
        var result = _sut.FromJwt(jwt, false); // false = read from token

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ClientId, result.Context!.ClientId);
        Assert.Equal(OrganisationId, result.Context.OrganisationId);
        Assert.Equal(scopesList, result.Context.Scopes);
    }

    [Fact]
    public void TestFromJwt_WithValidInputs_UsingAuthStoreScopes_ReturnsAuthContext()
    {
        // Arrange
        List<string> scopesList = ["file.read", "file.write"];

        var claims = new List<Claim>
        {
            new("client_id", ClientId),
            new("scope", "incorrect.scope"),
        };
        var jwt = new JwtSecurityToken(claims: claims);

        var validClient = new AuthClient
        {
            Enabled = true,
            OrganisationId = OrganisationId,
            AllowedScopes = scopesList,
        };

        _store.GetClientById(ClientId).Returns(validClient);

        // Act
        var result = _sut.FromJwt(jwt, true); // true = read from auth store

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ClientId, result.Context!.ClientId);
        Assert.Equal(OrganisationId, result.Context.OrganisationId);
        Assert.Equal(scopesList, result.Context.Scopes);
    }
}
