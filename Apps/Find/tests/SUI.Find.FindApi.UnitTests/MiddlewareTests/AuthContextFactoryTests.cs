using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.Find.FindApi.Middleware;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.FindApi.UnitTests.MiddlewareTests;

public class AuthContextFactoryTests
{
    private const string ClientId = "CLIENT-ID_EDUCATION-01";
    private const string OrganisationId = "EDUCATION-01";
    private readonly AuthContextFactory _sut;
    private readonly IAuthStoreService _store;

    public AuthContextFactoryTests()
    {
        _store = Substitute.For<IAuthStoreService>();
        _sut = new AuthContextFactory(_store);
    }

    [Fact]
    public void TestFromJwt_WhenClientIdAzpAndSubEmpty_ShouldThrowException()
    {
        // Arrange
        var claims = new List<Claim> { new("client_id", ""), new("azp", ""), new("sub", "") };
        var jwt = new JwtSecurityToken(claims: claims);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _sut.FromJwt(jwt, false));
        Assert.Contains("Token did not contain client_id, azp, or sub", ex.Message);
    }

    [Fact]
    public void TestFromJwt_WhenNoClientIdAzpOrSub_ShouldThrowException()
    {
        // Arrange
        var jwt = new JwtSecurityToken();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _sut.FromJwt(jwt, false));
        Assert.Contains("Token did not contain client_id, azp, or sub", ex.Message);
    }

    [Fact]
    public void TestFromJwt_WhenNoOrganisationIdForClientId_ShouldThrowException()
    {
        // Arrange
        var claims = new List<Claim> { new("client_id", ClientId) };
        var jwt = new JwtSecurityToken(claims: claims);
        _store.GetOrganisationIdForClientId(ClientId).Returns("");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _sut.FromJwt(jwt, false));
    }

    [Fact]
    public void TestFromJwt_WhenAzpAndSubPresent_UsesAzpAsClientId()
    {
        // Arrange
        var claims = new List<Claim> { new("azp", "AZP-CLIENT-01"), new("sub", "SUB-CLIENT-01") };
        var jwt = new JwtSecurityToken(claims: claims);
        _store.GetOrganisationIdForClientId("AZP-CLIENT-01").Returns("ORG-01");

        // Act
        var result = _sut.FromJwt(jwt, false);

        // Assert
        Assert.Equal("AZP-CLIENT-01", result.ClientId);
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
        _store.GetOrganisationIdForClientId("MAIN-CLIENT-01").Returns("ORG-01");

        // Act
        var result = _sut.FromJwt(jwt, false);

        // Assert
        Assert.Equal("MAIN-CLIENT-01", result.ClientId);
    }

    [Fact]
    public void TestFromJwt_WhenOnlySubPresent_UsesSubAsClientId()
    {
        // Arrange
        var claims = new List<Claim> { new("sub", "SUB-CLIENT-01") };
        var jwt = new JwtSecurityToken(claims: claims);
        _store.GetOrganisationIdForClientId("SUB-CLIENT-01").Returns("ORG-01");

        // Act
        var result = _sut.FromJwt(jwt, false);

        // Assert
        Assert.Equal("SUB-CLIENT-01", result.ClientId);
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

        _store.GetOrganisationIdForClientId(ClientId).Returns(OrganisationId);
        _store.GetScopesByClientId(Arg.Any<string>()).Throws<InvalidOperationException>(); // Should not use Auth Store for authorisation

        // Act
        var result = _sut.FromJwt(jwt, false);

        // Assert
        Assert.Equal(ClientId, result.ClientId);
        Assert.Equal(OrganisationId, result.OrganisationId);
        Assert.Equal(scopesList, result.Scopes);
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

        _store.GetOrganisationIdForClientId(ClientId).Returns(OrganisationId);
        _store.GetScopesByClientId(Arg.Any<string>()).Returns(scopesList);

        // Act
        var result = _sut.FromJwt(jwt, true);

        // Assert
        Assert.Equal(ClientId, result.ClientId);
        Assert.Equal(OrganisationId, result.OrganisationId);
        Assert.Equal(scopesList, result.Scopes);
    }
}
