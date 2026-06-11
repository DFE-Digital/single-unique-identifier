using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.Find.FindApi.Middleware;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.FindApi.UnitTests.MiddlewareTests;

public class AuthContextFactoryTests
{
    private readonly AuthContextFactory _sut = new();

    [Fact]
    public void TestFromJwt_WhenClientIdAndSubEmpty_ShouldThrowException()
    {
        var claims = new List<Claim> { new("client_id", ""), new("sub", "") };

        var jwt = new JwtSecurityToken(claims: claims);
        var store = Substitute.For<IAuthStoreService>();

        Assert.Throws<InvalidOperationException>(() => _sut.FromJwt(jwt, store, false));
    }

    [Fact]
    public void TestFromJwt_WhenNoClientIdOrSub_ShouldThrowException()
    {
        var jwt = new JwtSecurityToken();
        var store = Substitute.For<IAuthStoreService>();

        Assert.Throws<InvalidOperationException>(() => _sut.FromJwt(jwt, store, false));
    }

    [Fact]
    public void TestFromJwt_WhenNoOrganisationIdForClientId_ShouldThrowException()
    {
        var claims = new List<Claim> { new("client_id", "EDUCATION-01") };

        var jwt = new JwtSecurityToken(claims: claims);

        var store = Substitute.For<IAuthStoreService>();
        store.GetOrganisationIdForClientId("EDUCATION-01").Returns("");

        Assert.Throws<InvalidOperationException>(() => _sut.FromJwt(jwt, store, false));
    }

    [Fact]
    public void TestFromJwt_WithValidInputs_UsingTokenScopes_ReturnsAuthContext()
    {
        const string clientId = "EDUCATION-01";
        const string organisationId = "Edu-ORG-01";
        List<string> scopesList = ["file.read", "file.write"];

        var claims = new List<Claim>
        {
            new("client_id", clientId),
            new("scope", "file.read file.write"),
        };
        var jwt = new JwtSecurityToken(claims: claims);

        var store = Substitute.For<IAuthStoreService>();
        store.GetOrganisationIdForClientId("EDUCATION-01").Returns(organisationId);
        store.GetScopesByClientId(Arg.Any<string>()).Throws<InvalidOperationException>(); // Should not use Auth Store for authorisation

        var result = _sut.FromJwt(jwt, store, false);

        Assert.Equal(clientId, result.ClientId);
        Assert.Equal(organisationId, result.OrganisationId);
        Assert.Equal(scopesList, result.Scopes);
    }

    [Fact]
    public void TestFromJwt_WithValidInputs_UsingAuthStoreScopes_ReturnsAuthContext()
    {
        const string clientId = "EDUCATION-01";
        const string organisationId = "Edu-ORG-01";
        List<string> scopesList = ["file.read", "file.write"];

        var claims = new List<Claim>
        {
            new("client_id", clientId),
            new("scope", "incorrect.scope"),
        };
        var jwt = new JwtSecurityToken(claims: claims);

        var store = Substitute.For<IAuthStoreService>();
        store.GetOrganisationIdForClientId("EDUCATION-01").Returns(organisationId);
        store.GetScopesByClientId(Arg.Any<string>()).Returns(scopesList);

        var result = _sut.FromJwt(jwt, store, true);

        Assert.Equal(clientId, result.ClientId);
        Assert.Equal(organisationId, result.OrganisationId);
        Assert.Equal(scopesList, result.Scopes);
    }
}
