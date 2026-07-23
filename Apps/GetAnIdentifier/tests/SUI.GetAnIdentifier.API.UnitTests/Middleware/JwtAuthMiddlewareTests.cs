using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Shouldly;
using SUI.GetAnIdentifier.API.Configuration;
using SUI.GetAnIdentifier.API.Middleware;
using SUI.GetAnIdentifier.API.Models;
using SUI.GetAnIdentifier.Application.Constants;

namespace SUI.GetAnIdentifier.API.UnitTests.Middleware;

public class JwtAuthMiddlewareTests
{
    private readonly IAuthContextFactory _mockAuthContextFactory =
        Substitute.For<IAuthContextFactory>();
    private readonly IConfigurationManager<OpenIdConnectConfiguration> _mockConfigManager =
        Substitute.For<IConfigurationManager<OpenIdConnectConfiguration>>();
    private readonly IOptions<AuthSettings> _mockOptions = Substitute.For<IOptions<AuthSettings>>();
    private readonly ILogger<JwtAuthMiddleware> _mockLogger = Substitute.For<
        ILogger<JwtAuthMiddleware>
    >();

    private readonly AuthSettings _authSettings;
    private readonly RSA _genuineRsa;
    private readonly string _genuineKid = "kid-genuine-01";

    private bool _nextExecuted;

    private JwtAuthMiddlewareTests()
    {
        _genuineRsa = RSA.Create(2048);
        _authSettings = new AuthSettings
        {
            Issuer = "https://sandbox.api.example.gov.uk/find-a-record/auth",
            Audience = "sui-find-a-record-api",
            UseAuthStoreForAuthorisation = false, // Explicitly set for mocked AuthContextFactory predictability
        };
        _mockOptions.Value.Returns(_authSettings);
    }

    #region Shared Helpers

    private void AssertAccessAllowed(FunctionContext context, AuthContext expectedAuthContext)
    {
        Assert.True(_nextExecuted);
        Assert.True(context.Items.ContainsKey(ApplicationConstants.Auth.AuthContextKey));

        Assert.Null(context.GetInvocationResult().Value);

        Assert.Equal(expectedAuthContext, context.Items[ApplicationConstants.Auth.AuthContextKey]);
    }

    private void AssertAccessDenied(
        FunctionContext context,
        string expectedResponseContains,
        string? expectedSecurityErrorContains = null
    )
    {
        Assert.False(_nextExecuted);
        Assert.False(context.Items.ContainsKey(ApplicationConstants.Auth.AuthContextKey));

        Assert.NotNull(context.GetInvocationResult().Value);

        var responseData = Assert.IsType<HttpResponseData>(
            context.GetInvocationResult().Value,
            exactMatch: false
        );

        Assert.Equal(HttpStatusCode.Unauthorized, responseData.StatusCode);

        responseData.Body.Seek(0, SeekOrigin.Begin);
        using var streamReader = new StreamReader(responseData.Body);
        var responseBody = streamReader.ReadToEnd();

        responseBody.ShouldContain(expectedResponseContains);

        if (expectedSecurityErrorContains != null)
        {
            _mockLogger
                .Received(1)
                .Log(
                    LogLevel.Warning,
                    0,
                    Arg.Any<object>(),
                    Arg.Is<SecurityTokenException>(e =>
                        e!.Message.Contains(expectedSecurityErrorContains)
                    ),
                    Arg.Any<Func<object, Exception?, string>>()
                );
        }
    }

    private static string GenerateSymmetricToken(
        string issuer,
        string audience,
        string signingKey,
        string clientId,
        string scope = "fetch-record.read"
    )
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[] { new Claim("client_id", clientId), new Claim("scp", scope) };

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateAsymmetricToken(
        RSA rsaKey,
        string kid,
        string? issuer = null,
        string? audience = null,
        DateTime? notBefore = null,
        DateTime? expires = null,
        bool stripSignature = false,
        bool modifyPayload = false,
        bool modifyHeader = false,
        string scope = "get-an-identifier.read"
    )
    {
        var key = new RsaSecurityKey(rsaKey) { KeyId = kid };
        var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
        var claims = new[] { new Claim("client_id", "clientId"), new Claim("scp", scope) };

        var jwtToken = new JwtSecurityToken(
            issuer: issuer ?? _authSettings.Issuer,
            audience: audience ?? _authSettings.Audience,
            claims: claims,
            notBefore: notBefore ?? DateTime.UtcNow.AddMinutes(-5),
            expires: expires ?? DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(jwtToken);

        if (stripSignature)
        {
            return $"{tokenString.Split('.')[0]}.{tokenString.Split('.')[1]}.";
        }

        if (modifyPayload)
        {
            return $"{tokenString.Split('.')[0]}.{Base64UrlEncoder.Encode("{\"acu_bad\":\"true\"}")}.{tokenString.Split('.')[2]}";
        }

        if (modifyHeader)
        {
            var badHeader = Base64UrlEncoder.Encode(
                Base64UrlEncoder.Decode(tokenString.Split('.')[0]).Replace("JWT", "BAD")
            );
            return $"{badHeader}.{tokenString.Split('.')[1]}.{tokenString.Split('.')[2]}";
        }

        return tokenString;
    }

    private static OpenIdConnectConfiguration CreateOidcConfig(RSA rsaKey, string kid)
    {
        var config = new OpenIdConnectConfiguration();
        config.SigningKeys.Add(new RsaSecurityKey(rsaKey) { KeyId = kid });
        return config;
    }

    private static FunctionContext CreateMockFunctionContext(string? authHeaderValue)
    {
        var context = Substitute.For<FunctionContext>();
        var features = Substitute.For<IInvocationFeatures>();
        var requestFeature = Substitute.For<IHttpRequestDataFeature>();

        var requestData = Substitute.For<Microsoft.Azure.Functions.Worker.Http.HttpRequestData>(
            context
        );
        var responseData = Substitute.For<HttpResponseData>(context);

        var requestHeaders = new HttpHeadersCollection();
        if (authHeaderValue != null)
            requestHeaders.Add("Authorization", authHeaderValue);
        requestData.Headers.Returns(requestHeaders);

        var responseHeaders = new HttpHeadersCollection();
        responseData.Headers.Returns(responseHeaders);

        requestData.Url.Returns(new Uri("https://mock.gov.uk/api/v1/searches"));
        requestData.CreateResponse().Returns(responseData);
        responseData.Body.Returns(new MemoryStream());

        var invocationResult = Substitute.For<InvocationResult>();
        context.GetInvocationResult().Returns(invocationResult);

        requestFeature
            .GetHttpRequestDataAsync(context)
            .Returns(
                ValueTask.FromResult<Microsoft.Azure.Functions.Worker.Http.HttpRequestData?>(
                    requestData
                )
            );
        features.Get<IHttpRequestDataFeature>().Returns(requestFeature);
        context.Features.Returns(features);

        var items = new ConcurrentDictionary<object, object>();
        context.Items.Returns(items);
        context.FunctionDefinition.EntryPoint.Returns(
            "SUI.GetAnIdentifier.API.Functions.GetAnIdentifierFunction.GetAnIdentifier"
        );

        var workerOptions = new WorkerOptions
        {
            Serializer = Azure.Core.Serialization.JsonObjectSerializer.Default,
        };
        var mockWorkerOptionsContainer = Substitute.For<IOptions<WorkerOptions>>();
        mockWorkerOptionsContainer.Value.Returns(workerOptions);

        var mockServiceProvider = Substitute.For<IServiceProvider>();
        mockServiceProvider
            .GetService(typeof(IOptions<WorkerOptions>))
            .Returns(mockWorkerOptionsContainer);
        context.InstanceServices.Returns(mockServiceProvider);

        return context;
    }

    private Task Next(FunctionContext context)
    {
        _nextExecuted = true;
        return Task.CompletedTask;
    }

    #endregion

    public class GeneralVerificationTests : JwtAuthMiddlewareTests
    {
        [Theory]
        [InlineData("swagger")]
        [InlineData("openapi")]
        [InlineData("v1/auth/token")]
        [InlineData("health")]
        public async Task TestInvoke_WithNoAuthEndpoints_SkipsMethod(string endpoint)
        {
            // Arrange
            var context = CreateMockFunctionContext(null);
            var req = await context.GetHttpRequestDataAsync();
            req!.Url.Returns(new Uri("https://mock.gov.uk/api/" + endpoint));
            var sut = new JwtAuthMiddleware(
                _mockAuthContextFactory,
                _mockConfigManager,
                _mockOptions,
                _mockLogger
            );

            // Act
            await sut.Invoke(context, Next);

            // Assert
            Assert.True(_nextExecuted);
        }

        [Fact]
        public async Task TestInvoke_WithNoAuthHeaders_ReturnsProblemResponse()
        {
            // Arrange
            var context = CreateMockFunctionContext(null);
            var sut = new JwtAuthMiddleware(
                _mockAuthContextFactory,
                _mockConfigManager,
                _mockOptions,
                _mockLogger
            );

            // Act
            await sut.Invoke(context, Next);

            // Assert
            AssertAccessDenied(context, "Missing Authorization header");
        }

        [Fact]
        public async Task TestInvoke_WithInvalidAuthHeaders_ReturnsProblemResponse()
        {
            // Arrange
            var context = CreateMockFunctionContext("Invalid");
            var sut = new JwtAuthMiddleware(
                _mockAuthContextFactory,
                _mockConfigManager,
                _mockOptions,
                _mockLogger
            );

            // Act
            await sut.Invoke(context, Next);

            // Assert
            Assert.Equal(
                HttpStatusCode.Unauthorized,
                ((HttpResponseData)context.GetInvocationResult().Value!).StatusCode
            );
            AssertAccessDenied(context, "Invalid Authorization header");
        }

        [Fact]
        public async Task TestInvoke_WhenClientIsDisabled_ShouldDenyAccess()
        {
            // Arrange
            var token = GenerateAsymmetricToken(_genuineRsa, _genuineKid);
            var config = CreateOidcConfig(_genuineRsa, _genuineKid);
            _mockConfigManager.GetConfigurationAsync(Arg.Any<CancellationToken>()).Returns(config);

            // Mock the explicitly typed Failure state
            var authResult = AuthResult.Failure(
                AuthFailureReason.ClientDisabled,
                "Client is disabled."
            );

            _mockAuthContextFactory
                .FromJwt(Arg.Any<JwtSecurityToken>(), Arg.Any<bool>())
                .Returns(authResult);

            var context = CreateMockFunctionContext($"Bearer {token}");
            var sut = new JwtAuthMiddleware(
                _mockAuthContextFactory,
                _mockConfigManager,
                _mockOptions,
                _mockLogger
            );

            // Act
            await sut.Invoke(context, Next);

            // Assert
            AssertAccessDenied(context, "Client is disabled.");
        }
    }

    public class SymmetricVerificationTests : JwtAuthMiddlewareTests
    {
        [Fact]
        public async Task TestInvoke_WithSymmetricToken_ReturnsProblemResponse()
        {
            // Arrange
            var config = CreateOidcConfig(_genuineRsa, _genuineKid);
            _mockConfigManager.GetConfigurationAsync(Arg.Any<CancellationToken>()).Returns(config);

            var token = GenerateSymmetricToken(
                _authSettings.Issuer,
                _authSettings.Audience,
                "SecretKeyShouldBeLongEnoughToPass12345!",
                "clientId"
            );

            var context = CreateMockFunctionContext($"Bearer {token}");

            var sut = new JwtAuthMiddleware(
                _mockAuthContextFactory,
                _mockConfigManager,
                _mockOptions,
                _mockLogger
            );

            // Act
            await sut.Invoke(context, Next);

            // Assert
            AssertAccessDenied(context, "Token validation failed", "Signature validation failed");
        }
    }

    public class AsymmetricVerificationTests : JwtAuthMiddlewareTests
    {
        [Fact]
        public async Task Scenario0_ValidToken_WhenScopesIncorrect_ShouldDenyAccess()
        {
            // Arrange
            var token = GenerateAsymmetricToken(_genuineRsa, _genuineKid, scope: "wrong.scope");
            var config = CreateOidcConfig(_genuineRsa, _genuineKid);
            _mockConfigManager.GetConfigurationAsync(Arg.Any<CancellationToken>()).Returns(config);

            var authContext = new AuthContext("clientId", "organisationId", ["wrong.scope"]);
            var authResult = AuthResult.Success(authContext);

            _mockAuthContextFactory
                .FromJwt(Arg.Any<JwtSecurityToken>(), Arg.Any<bool>())
                .Returns(authResult);

            var context = CreateMockFunctionContext($"Bearer {token}");
            var sut = new JwtAuthMiddleware(
                _mockAuthContextFactory,
                _mockConfigManager,
                _mockOptions,
                _mockLogger
            );

            // Act
            await sut.Invoke(context, Next);

            // Assert
            AssertAccessDenied(context, "Insufficient scope for this operation");
        }

        [Fact]
        public async Task Scenario1_ValidToken_ShouldAllowAccessAndPopulateAuthContext()
        {
            // Arrange
            var token = GenerateAsymmetricToken(_genuineRsa, _genuineKid);
            var config = CreateOidcConfig(_genuineRsa, _genuineKid);
            _mockConfigManager.GetConfigurationAsync(Arg.Any<CancellationToken>()).Returns(config);

            var authContext = new AuthContext(
                "clientId",
                "organisationId",
                ["get-an-identifier.read"]
            );
            var authResult = AuthResult.Success(authContext);

            _mockAuthContextFactory
                .FromJwt(Arg.Any<JwtSecurityToken>(), Arg.Any<bool>())
                .Returns(authResult);

            var context = CreateMockFunctionContext($"Bearer {token}");
            var sut = new JwtAuthMiddleware(
                _mockAuthContextFactory,
                _mockConfigManager,
                _mockOptions,
                _mockLogger
            );

            // Act
            await sut.Invoke(context, Next);

            // Assert
            AssertAccessAllowed(context, authContext);
        }

        [Fact]
        public async Task Scenario2_KidMissingInitially_ShouldForceRefreshAndSucceedOnRetry()
        {
            // Arrange
            var token = GenerateAsymmetricToken(_genuineRsa, _genuineKid);
            var emptyConfig = new OpenIdConnectConfiguration();
            var refreshedConfig = CreateOidcConfig(_genuineRsa, _genuineKid);
            _mockConfigManager
                .GetConfigurationAsync(Arg.Any<CancellationToken>())
                .Returns(emptyConfig, refreshedConfig);

            var authContext = new AuthContext(
                "clientId",
                "organisationId",
                ["get-an-identifier.read"]
            );
            var authResult = AuthResult.Success(authContext);

            _mockAuthContextFactory
                .FromJwt(Arg.Any<JwtSecurityToken>(), Arg.Any<bool>())
                .Returns(authResult);

            var context = CreateMockFunctionContext($"Bearer {token}");
            var sut = new JwtAuthMiddleware(
                _mockAuthContextFactory,
                _mockConfigManager,
                _mockOptions,
                _mockLogger
            );

            // Act
            await sut.Invoke(context, Next);

            // Assert
            _mockConfigManager.Received(1).RequestRefresh();
            AssertAccessAllowed(context, authContext);
        }

        [Fact]
        public async Task Scenario3_KidMissingEvenAfterForceRefresh_ShouldDenyAccess()
        {
            // Arrange
            var token = GenerateAsymmetricToken(_genuineRsa, _genuineKid);
            var emptyConfig = new OpenIdConnectConfiguration();
            _mockConfigManager
                .GetConfigurationAsync(Arg.Any<CancellationToken>())
                .Returns(emptyConfig);

            var context = CreateMockFunctionContext($"Bearer {token}");
            var sut = new JwtAuthMiddleware(
                _mockAuthContextFactory,
                _mockConfigManager,
                _mockOptions,
                _mockLogger
            );

            // Act
            await sut.Invoke(context, Next);

            // Assert
            _mockConfigManager.Received(1).RequestRefresh();
            AssertAccessDenied(context, "Token validation failed", "No security keys");
        }

        [Fact]
        public async Task Scenario4_ModifiedPayload_ShouldDenyAccess()
        {
            // Arrange
            var token = GenerateAsymmetricToken(_genuineRsa, _genuineKid, modifyPayload: true);
            var config = CreateOidcConfig(_genuineRsa, _genuineKid);
            _mockConfigManager.GetConfigurationAsync(Arg.Any<CancellationToken>()).Returns(config);

            var context = CreateMockFunctionContext($"Bearer {token}");
            var sut = new JwtAuthMiddleware(
                _mockAuthContextFactory,
                _mockConfigManager,
                _mockOptions,
                _mockLogger
            );

            // Act
            await sut.Invoke(context, Next);

            // Assert
            AssertAccessDenied(context, "Token validation failed", "Signature validation failed");
        }

        [Fact]
        public async Task Scenario5_ModifiedHeader_ShouldDenyAccess()
        {
            // Arrange
            var token = GenerateAsymmetricToken(_genuineRsa, _genuineKid, modifyHeader: true);
            var config = CreateOidcConfig(_genuineRsa, _genuineKid);
            _mockConfigManager.GetConfigurationAsync(Arg.Any<CancellationToken>()).Returns(config);

            var context = CreateMockFunctionContext($"Bearer {token}");
            var sut = new JwtAuthMiddleware(
                _mockAuthContextFactory,
                _mockConfigManager,
                _mockOptions,
                _mockLogger
            );

            // Act
            await sut.Invoke(context, Next);

            // Assert
            AssertAccessDenied(context, "Token validation failed", "Signature validation failed");
        }

        [Fact]
        public async Task Scenario6_TokenNotActiveYet_ShouldDenyAccess()
        {
            // Arrange
            var token = GenerateAsymmetricToken(
                _genuineRsa,
                _genuineKid,
                notBefore: DateTime.UtcNow.AddHours(2),
                expires: DateTime.UtcNow.AddHours(3)
            );
            var config = CreateOidcConfig(_genuineRsa, _genuineKid);
            _mockConfigManager.GetConfigurationAsync(Arg.Any<CancellationToken>()).Returns(config);

            var context = CreateMockFunctionContext($"Bearer {token}");
            var sut = new JwtAuthMiddleware(
                _mockAuthContextFactory,
                _mockConfigManager,
                _mockOptions,
                _mockLogger
            );

            // Act
            await sut.Invoke(context, Next);

            // Assert
            AssertAccessDenied(context, "Token validation failed", "The token is not yet valid");
        }

        [Fact]
        public async Task Scenario7_TokenExpired_ShouldDenyAccess()
        {
            // Arrange
            var token = GenerateAsymmetricToken(
                _genuineRsa,
                _genuineKid,
                notBefore: DateTime.UtcNow.AddHours(-3),
                expires: DateTime.UtcNow.AddHours(-2)
            );
            var config = CreateOidcConfig(_genuineRsa, _genuineKid);
            _mockConfigManager.GetConfigurationAsync(Arg.Any<CancellationToken>()).Returns(config);

            var context = CreateMockFunctionContext($"Bearer {token}");
            var sut = new JwtAuthMiddleware(
                _mockAuthContextFactory,
                _mockConfigManager,
                _mockOptions,
                _mockLogger
            );

            // Act
            await sut.Invoke(context, Next);

            // Assert
            AssertAccessDenied(context, "Token validation failed", "The token is expired");
        }

        [Fact]
        public async Task Scenario8_StrippedSignature_ShouldDenyAccess()
        {
            // Arrange
            var token = GenerateAsymmetricToken(_genuineRsa, _genuineKid, stripSignature: true);
            var config = CreateOidcConfig(_genuineRsa, _genuineKid);
            _mockConfigManager.GetConfigurationAsync(Arg.Any<CancellationToken>()).Returns(config);

            var context = CreateMockFunctionContext($"Bearer {token}");
            var sut = new JwtAuthMiddleware(
                _mockAuthContextFactory,
                _mockConfigManager,
                _mockOptions,
                _mockLogger
            );

            // Act
            await sut.Invoke(context, Next);

            // Assert
            AssertAccessDenied(
                context,
                "Token validation failed",
                "token does not have a signature"
            );
        }

        [Fact]
        public async Task Scenario9_ForgedPrivateKeySignature_ShouldDenyAccess()
        {
            // Arrange
            using var forgedRsaKey = RSA.Create(2048);
            var token = GenerateAsymmetricToken(forgedRsaKey, _genuineKid);
            var config = CreateOidcConfig(_genuineRsa, _genuineKid);
            _mockConfigManager.GetConfigurationAsync(Arg.Any<CancellationToken>()).Returns(config);

            var context = CreateMockFunctionContext($"Bearer {token}");
            var sut = new JwtAuthMiddleware(
                _mockAuthContextFactory,
                _mockConfigManager,
                _mockOptions,
                _mockLogger
            );

            // Act
            await sut.Invoke(context, Next);

            // Assert
            AssertAccessDenied(context, "Token validation failed", "Signature validation failed");
        }

        [Fact]
        public async Task Scenario10_InvalidIssuer_ShouldDenyAccess()
        {
            // Arrange
            var token = GenerateAsymmetricToken(
                _genuineRsa,
                _genuineKid,
                issuer: "https://untrusted-issuer.com"
            );
            var config = CreateOidcConfig(_genuineRsa, _genuineKid);
            _mockConfigManager.GetConfigurationAsync(Arg.Any<CancellationToken>()).Returns(config);

            var context = CreateMockFunctionContext($"Bearer {token}");
            var sut = new JwtAuthMiddleware(
                _mockAuthContextFactory,
                _mockConfigManager,
                _mockOptions,
                _mockLogger
            );

            // Act
            await sut.Invoke(context, Next);

            // Assert
            AssertAccessDenied(context, "Token validation failed", "Issuer validation failed");
        }

        [Fact]
        public async Task Scenario11_InvalidAudience_ShouldDenyAccess()
        {
            // Arrange
            var token = GenerateAsymmetricToken(
                _genuineRsa,
                _genuineKid,
                audience: "malicious-intercept-audience"
            );
            var config = CreateOidcConfig(_genuineRsa, _genuineKid);
            _mockConfigManager.GetConfigurationAsync(Arg.Any<CancellationToken>()).Returns(config);

            var context = CreateMockFunctionContext($"Bearer {token}");
            var sut = new JwtAuthMiddleware(
                _mockAuthContextFactory,
                _mockConfigManager,
                _mockOptions,
                _mockLogger
            );

            // Act
            await sut.Invoke(context, Next);

            // Assert
            AssertAccessDenied(context, "Token validation failed", "Audience validation failed");
        }
    }
}
