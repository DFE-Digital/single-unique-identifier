using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SUI.Find.FindApi.Middleware;
using SUI.Find.Infrastructure.Models;

namespace SUI.Find.FindApi.UnitTests.MiddlewareTests;

public class AuthContextFactoryTests
{
    private readonly AuthContextFactory _sut = new();

    [Fact]
    public void TestFromJwt_WhenClientIdAndSubEmpty_ShouldThrowException()
    {
        var claims = new List<Claim> { new("client_id", ""), new("sub", "") };

        var jwt = new JwtSecurityToken(claims: claims);
        var store = new AuthStore { DefaultTokenLifetimeMinutes = 60 };

        Assert.Throws<InvalidOperationException>(() => _sut.FromJwt(jwt, store));
    }

    [Fact]
    public void TestFromJwt_WhenNoClientIdOrSub_ShouldThrowException()
    {
        var jwt = new JwtSecurityToken();
        var store = new AuthStore { DefaultTokenLifetimeMinutes = 60 };

        Assert.Throws<InvalidOperationException>(() => _sut.FromJwt(jwt, store));
    }

    [Fact]
    public void TestFromJwt_WhenNoClientIdInAuthStore_ShouldThrowException()
    {
        var claims = new List<Claim> { new("client_id", "EDUCATION-01") };

        var jwt = new JwtSecurityToken(claims: claims);

        var clients = new List<AuthClient>
        {
            new()
            {
                ClientId = "POLICE-01",
                Enabled = true,
                OrganisationId = "POL-ORG-01",
                ClientSecret = "secret",
                AllowedScopes = ["file.read", "file.write"],
            },
        };

        var store = new AuthStore { Clients = clients, DefaultTokenLifetimeMinutes = 60 };

        Assert.Throws<InvalidOperationException>(() => _sut.FromJwt(jwt, store));
    }

    [Fact]
    public void TestFromJwt_WhenNoOrganisationIdForClientId_ShouldThrowException()
    {
        var claims = new List<Claim> { new("client_id", "EDUCATION-01") };

        var jwt = new JwtSecurityToken(claims: claims);

        var clients = new List<AuthClient>
        {
            new()
            {
                ClientId = "EDUCATION-01",
                Enabled = true,
                OrganisationId = "",
                ClientSecret = "secret",
                AllowedScopes = ["file.read", "file.write"],
            },
        };

        var store = new AuthStore { Clients = clients, DefaultTokenLifetimeMinutes = 60 };

        Assert.Throws<InvalidOperationException>(() => _sut.FromJwt(jwt, store));
    }

    [Fact]
    public void TestFromJwt_WithValidInputs_ReturnsAuthContext()
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

        var clients = new List<AuthClient>
        {
            new()
            {
                ClientId = clientId,
                Enabled = true,
                OrganisationId = organisationId,
                ClientSecret = "secret",
                AllowedScopes = ["file.read", "file.write", "record.read"],
            },
        };

        var store = new AuthStore { Clients = clients, DefaultTokenLifetimeMinutes = 60 };

        var result = _sut.FromJwt(jwt, store);

        Assert.Equal(clientId, result.ClientId);
        Assert.Equal(organisationId, result.OrganisationId);
        Assert.Equal(scopesList, result.Scopes);
    }
}
