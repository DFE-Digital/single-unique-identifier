using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.Find.Application.Constants;
using SUI.Find.FindApi.Middleware;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.UnitTests.Mocks;
using SUI.Find.Infrastructure.Models;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.FindApi.UnitTests.MiddlewareTests;

public class JwtAuthMiddlewareTests
{
    private readonly FunctionContext _context = Substitute.For<FunctionContext>();
    private readonly IAuthStoreService _authStoreService = Substitute.For<IAuthStoreService>();
    private readonly IAuthContextFactory _authContextFactory =
        Substitute.For<IAuthContextFactory>();
    private readonly JwtSecurityTokenHandler _handler = Substitute.For<JwtSecurityTokenHandler>();

    [Theory]
    [InlineData("swagger")]
    [InlineData("openapi")]
    [InlineData("v1/auth/token")]
    [InlineData("health")]
    public async Task TestInvoke_WithNoAuthEndpoints_SkipsMethod(string endpoint)
    {
        // Arrange
        var sut = new JwtAuthMiddleware(_authStoreService, _authContextFactory, _handler);
        var request = Substitute.For<HttpRequestData>(_context);
        request.Url.Returns(new Uri("https://mock.gov.uk/api/" + endpoint));
        _context.GetHttpRequestDataAsync().Returns(request);

        // Act
        await sut.Invoke(_context, Next);

        // Assert
        await _authStoreService.DidNotReceive().GetAuthStoreAsync();
        _handler
            .DidNotReceive()
            .ValidateToken(Arg.Any<string>(), Arg.Any<TokenValidationParameters>(), out _);
        _authContextFactory
            .DidNotReceive()
            .FromJwt(Arg.Any<JwtSecurityToken>(), Arg.Any<AuthStore>());
    }

    [Fact]
    public async Task TestInvoke_WithNoAuthHeaders_ReturnsProblemResponse()
    {
        // Arrange
        InitialiseRequest([]);
        var sut = new JwtAuthMiddleware(_authStoreService, _authContextFactory, _handler);

        // Act
        await sut.Invoke(_context, Next);

        // Assert
        var invocationResult = Assert.IsType<HttpResponseData>(
            _context.GetInvocationResult().Value,
            exactMatch: false
        );
        Assert.Equal(HttpStatusCode.Unauthorized, invocationResult.StatusCode);

        await _authStoreService.DidNotReceive().GetAuthStoreAsync();
        _handler
            .DidNotReceive()
            .ValidateToken(Arg.Any<string>(), Arg.Any<TokenValidationParameters>(), out _);
        _authContextFactory
            .DidNotReceive()
            .FromJwt(Arg.Any<JwtSecurityToken>(), Arg.Any<AuthStore>());
    }

    [Fact]
    public async Task TestInvoke_WithInvalidAuthHeaders_ReturnsProblemResponse()
    {
        // Arrange
        InitialiseRequest(new HttpHeadersCollection { { "Authorization", "Invalid" } });
        var sut = new JwtAuthMiddleware(_authStoreService, _authContextFactory, _handler);

        // Act
        await sut.Invoke(_context, Next);

        // Assert
        var invocationResult = Assert.IsType<HttpResponseData>(
            _context.GetInvocationResult().Value,
            exactMatch: false
        );
        Assert.Equal(HttpStatusCode.Unauthorized, invocationResult.StatusCode);

        await _authStoreService.DidNotReceive().GetAuthStoreAsync();
        _handler
            .DidNotReceive()
            .ValidateToken(Arg.Any<string>(), Arg.Any<TokenValidationParameters>(), out _);
        _authContextFactory
            .DidNotReceive()
            .FromJwt(Arg.Any<JwtSecurityToken>(), Arg.Any<AuthStore>());
    }

    [Fact]
    public async Task TestInvoke_HandlesErrorsFromTokenHandler()
    {
        // Arrange
        InitialiseRequest(new HttpHeadersCollection { { "Authorization", "Bearer token" } });

        var clients = new List<AuthClient>
        {
            new()
            {
                ClientId = "clientId",
                ClientSecret = "clientSecret",
                AllowedScopes = ["file.read", "file.write"],
                Enabled = true,
                OrganisationId = "organisationId",
            },
        };

        var store = new AuthStore
        {
            Audience = "Audience",
            Issuer = "Issuer",
            Clients = clients,
            DefaultTokenLifetimeMinutes = 60,
            SigningKey = "SigningKey",
        };

        _authStoreService.GetAuthStoreAsync().Returns(store);

        _handler
            .ValidateToken(
                Arg.Any<string>(),
                Arg.Any<TokenValidationParameters>(),
                out Arg.Any<SecurityToken>()
            )
            .Throws<SecurityTokenException>();

        var sut = new JwtAuthMiddleware(_authStoreService, _authContextFactory, _handler);

        // Act
        await sut.Invoke(_context, Next);

        // Assert
        var invocationResult = Assert.IsType<HttpResponseData>(
            _context.GetInvocationResult().Value,
            exactMatch: false
        );
        Assert.Equal(HttpStatusCode.Unauthorized, invocationResult.StatusCode);

        await _authStoreService.Received(1).GetAuthStoreAsync();
        _handler
            .Received(1)
            .ValidateToken(Arg.Any<string>(), Arg.Any<TokenValidationParameters>(), out _);
        _authContextFactory
            .DidNotReceive()
            .FromJwt(Arg.Any<JwtSecurityToken>(), Arg.Any<AuthStore>());
    }

    [Fact]
    public async Task TestInvoke_WhenScopesIncorrect_ReturnsProblemResponse()
    {
        // Arrange
        InitialiseRequest(new HttpHeadersCollection { { "Authorization", "Bearer token" } });

        var clients = new List<AuthClient>
        {
            new()
            {
                ClientId = "clientId",
                ClientSecret = "clientSecret",
                AllowedScopes = ["file.read"],
                Enabled = true,
                OrganisationId = "organisationId",
            },
        };

        var store = new AuthStore
        {
            Audience = "Audience",
            Issuer = "Issuer",
            Clients = clients,
            DefaultTokenLifetimeMinutes = 60,
            SigningKey = "SigningKey",
        };

        _authStoreService.GetAuthStoreAsync().Returns(store);

        _handler
            .ValidateToken(
                Arg.Any<string>(),
                Arg.Any<TokenValidationParameters>(),
                out Arg.Any<SecurityToken>()
            )
            .Returns(x =>
            {
                x[2] = new JwtSecurityToken();
                return new ClaimsPrincipal();
            });

        _authContextFactory
            .FromJwt(Arg.Any<JwtSecurityToken>(), Arg.Any<AuthStore>())
            .Returns(new AuthContext("clientId", "organisationId", ["file.write"]));

        var sut = new JwtAuthMiddleware(_authStoreService, _authContextFactory, _handler);

        _context.FunctionDefinition.EntryPoint.Returns(
            "SUI.Find.FindApi.Functions.HttpFunctions.FetchRecordFunction.FetchRecord"
        );

        // Act
        await sut.Invoke(_context, Next);

        // Assert
        var invocationResult = Assert.IsType<HttpResponseData>(
            _context.GetInvocationResult().Value,
            exactMatch: false
        );
        Assert.Equal(HttpStatusCode.Unauthorized, invocationResult.StatusCode);

        await _authStoreService.Received(1).GetAuthStoreAsync();
        _handler
            .Received(1)
            .ValidateToken(Arg.Any<string>(), Arg.Any<TokenValidationParameters>(), out _);
        _authContextFactory.Received(1).FromJwt(Arg.Any<JwtSecurityToken>(), Arg.Any<AuthStore>());
    }

    [Fact]
    public async Task TestInvoke_WithValidInputs_ReturnsAuthContext()
    {
        // Arrange
        InitialiseRequest(new HttpHeadersCollection { { "Authorization", "Bearer token" } });

        var clients = new List<AuthClient>
        {
            new()
            {
                ClientId = "clientId",
                ClientSecret = "clientSecret",
                AllowedScopes = ["fetch-record.read"],
                Enabled = true,
                OrganisationId = "organisationId",
            },
        };

        var store = new AuthStore
        {
            Audience = "Audience",
            Issuer = "Issuer",
            Clients = clients,
            DefaultTokenLifetimeMinutes = 60,
            SigningKey = "SigningKey",
        };

        _authStoreService.GetAuthStoreAsync().Returns(store);

        _handler
            .ValidateToken(
                Arg.Any<string>(),
                Arg.Any<TokenValidationParameters>(),
                out Arg.Any<SecurityToken>()
            )
            .Returns(x =>
            {
                x[2] = new JwtSecurityToken();
                return new ClaimsPrincipal();
            });

        var authContext = new AuthContext("clientId", "organisationId", ["fetch-record.read"]);

        _authContextFactory
            .FromJwt(Arg.Any<JwtSecurityToken>(), Arg.Any<AuthStore>())
            .Returns(authContext);

        var sut = new JwtAuthMiddleware(_authStoreService, _authContextFactory, _handler);

        _context.FunctionDefinition.EntryPoint.Returns(
            "SUI.Find.FindApi.Functions.HttpFunctions.FetchRecordFunction.FetchRecord"
        );

        // Act
        await sut.Invoke(_context, Next);

        // Assert
        await _authStoreService.Received(1).GetAuthStoreAsync();
        _handler
            .Received(1)
            .ValidateToken(Arg.Any<string>(), Arg.Any<TokenValidationParameters>(), out _);
        _authContextFactory.Received(1).FromJwt(Arg.Any<JwtSecurityToken>(), Arg.Any<AuthStore>());

        Assert.Equal(authContext, _context.Items[ApplicationConstants.Auth.AuthContextKey]);
    }

    private void InitialiseRequest(HttpHeadersCollection headers)
    {
        var request = Substitute.For<HttpRequestData>(_context);
        request.Url.Returns(new Uri("https://mock.gov.uk/api/v1/searches"));
        request.Headers.Returns(headers);
        request.CreateResponse().Returns(new MockHttpResponseData(_context));
        _context.GetHttpRequestDataAsync().Returns(request);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddFunctionsWorkerDefaults();
        _context.InstanceServices.Returns(serviceCollection.BuildServiceProvider());
    }

    private static Task Next(FunctionContext context)
    {
        return Task.CompletedTask;
    }
}
