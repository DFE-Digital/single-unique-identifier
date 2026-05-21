using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.AuthEmulator.Controllers;
using SUI.AuthEmulator.Models;
using SUI.AuthEmulator.Services;

namespace SUI.AuthEmulator.UnitTests;

public class AuthControllerTests
{
    private readonly AuthController _sut;
    private readonly IAuthStoreService _authStoreService = Substitute.For<IAuthStoreService>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();

    public AuthControllerTests()
    {
        var logger = Substitute.For<ILogger<AuthController>>();
        _sut = new AuthController(logger, _authStoreService, _jwtTokenService)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
    }

    [Fact]
    public async Task ShouldReturnProblemResponse_WhenNoFormDetailsProvided()
    {
        // Arrange
        // Act
        var result = await _sut.AuthToken();

        // Assert
        Assert.NotNull(result.Result);
        Assert.IsType<ProblemHttpResult>(result.Result);
        var problemHttpResult = (ProblemHttpResult)result.Result;
        Assert.Equal(400, problemHttpResult.StatusCode);
        Assert.Equal("Invalid request", problemHttpResult.ProblemDetails.Title);
        Assert.Equal(
            "Missing or malformed authentication details.",
            problemHttpResult.ProblemDetails.Detail
        );
    }

    [Fact]
    public async Task ShouldReturnProblemResponse_WhenNoAuthorizationHeaderProvided()
    {
        // Arrange
        SetHeadersAndForm(
            form: new Dictionary<string, StringValues> { { "grant_type", "client_credentials" } },
            headers: new Dictionary<string, string>
            {
                { "Content-Type", "application/x-www-form-urlencoded" },
            }
        );

        // Act
        var result = await _sut.AuthToken();

        // Assert
        Assert.NotNull(result.Result);
        Assert.IsType<ProblemHttpResult>(result.Result);
        var problemHttpResult = (ProblemHttpResult)result.Result;
        Assert.Equal(400, problemHttpResult.StatusCode);
        Assert.Equal("Invalid request", problemHttpResult.ProblemDetails.Title);
        Assert.Equal(
            "Missing or malformed authentication details.",
            problemHttpResult.ProblemDetails.Detail
        );
    }

    [Fact]
    public async Task ShouldReturnProblemResponse_WhenClientSecretIsMissing()
    {
        // Arrange
        // Only clientId, no clientSecret
        const string credentials = "valid_client_id:";
        var base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

        SetHeadersAndForm(
            form: new Dictionary<string, StringValues> { { "grant_type", "client_credentials" } },
            headers: new Dictionary<string, string>
            {
                { "Content-Type", "application/x-www-form-urlencoded" },
                { "Authorization", $"Basic {base64Credentials}" },
            }
        );

        // Act
        var result = await _sut.AuthToken();

        // Assert
        Assert.NotNull(result.Result);
        Assert.IsType<ProblemHttpResult>(result.Result);
        var problemHttpResult = (ProblemHttpResult)result.Result;
        Assert.Equal(401, problemHttpResult.StatusCode);
        Assert.Equal("Unauthorized", problemHttpResult.ProblemDetails.Title);
        Assert.Equal("Invalid client credentials.", problemHttpResult.ProblemDetails.Detail);
    }

    [Fact]
    public async Task ShouldThrowException_WhenFileIsNotFound()
    {
        SetHeadersAndForm(
            form: new Dictionary<string, StringValues>
            {
                { "grant_type", "client_credentials" },
                { "scope", "file.read" },
            },
            headers: new Dictionary<string, string>
            {
                { "Content-Type", "application/x-www-form-urlencoded" },
                {
                    "Authorization",
                    "Basic "
                        + Convert.ToBase64String("valid_client_id:valid_client_secret"u8.ToArray())
                },
            }
        );

        _authStoreService
            .GetClientByCredentials("valid_client_id", "valid_client_secret")
            .Throws(new InvalidOperationException("Auth store file not found at: some/path"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _sut.AuthToken();
        });
    }

    [Fact]
    public async Task ShouldReturnProblem_WhenInvalidScopeProvided()
    {
        // Arrange
        SetHeadersAndForm(
            form: new Dictionary<string, StringValues>
            {
                { "grant_type", "client_credentials" },
                { "scope", "invalid.scope" },
            },
            headers: new Dictionary<string, string>
            {
                { "Content-Type", "application/x-www-form-urlencoded" },
                {
                    "Authorization",
                    "Basic "
                        + Convert.ToBase64String("valid_client_id:valid_client_secret"u8.ToArray())
                },
            }
        );
        _authStoreService
            .GetClientByCredentials("valid_client_id", "valid_client_secret")
            .Returns(
                Result<AuthClient>.Ok(
                    new AuthClient
                    {
                        ClientId = "valid_client_id",
                        ClientSecret = "valid_client_secret",
                        Enabled = true,
                        AllowedScopes = ["file.read", "file.write"],
                    }
                )
            );
        _jwtTokenService
            .GenerateToken("valid_client_id", Arg.Any<IReadOnlyList<string>>())
            .Returns("mocked_jwt_token");

        // Act
        var result = await _sut.AuthToken();

        // Assert
        Assert.NotNull(result.Result);
        Assert.IsType<ProblemHttpResult>(result.Result);
        var problemHttpResult = (ProblemHttpResult)result.Result;
        Assert.Equal(400, problemHttpResult.StatusCode);
        Assert.Equal("Invalid scope", problemHttpResult.ProblemDetails.Title);
        Assert.Equal(
            "Client is not permitted to request scope(s): invalid.scope.",
            problemHttpResult.ProblemDetails.Detail
        );
    }

    [Fact]
    public async Task ShouldReturnJwtTokenInResponse_WhenValidClientCredentialsProvided()
    {
        // Arrange
        SetHeadersAndForm(
            form: new Dictionary<string, StringValues>
            {
                { "grant_type", "client_credentials" },
                { "scope", "file.read" },
            },
            headers: new Dictionary<string, string>
            {
                { "Content-Type", "application/x-www-form-urlencoded" },
                {
                    "Authorization",
                    "Basic "
                        + Convert.ToBase64String("valid_client_id:valid_client_secret"u8.ToArray())
                },
            }
        );
        _authStoreService
            .GetClientByCredentials("valid_client_id", "valid_client_secret")
            .Returns(
                Result<AuthClient>.Ok(
                    new AuthClient
                    {
                        ClientId = "valid_client_id",
                        ClientSecret = "valid_client_secret",
                        Enabled = true,
                        AllowedScopes = ["file.read", "file.write"],
                    }
                )
            );
        _jwtTokenService
            .GenerateToken("valid_client_id", Arg.Any<IReadOnlyList<string>>())
            .Returns("mocked_jwt_token");

        // Act
        var result = await _sut.AuthToken();

        // Assert
        Assert.NotNull(result.Result);
        Assert.IsType<Ok<AuthTokenResponse>>(result.Result);
        var okHttpResult = (Ok<AuthTokenResponse>)result.Result;
        Assert.Equal(200, okHttpResult.StatusCode);
        Assert.NotNull(okHttpResult.Value);
        Assert.False(string.IsNullOrEmpty(okHttpResult.Value.AccessToken));
        Assert.Equal("file.read", okHttpResult.Value.Scope);
        Assert.Equal("Bearer", okHttpResult.Value.TokenType);
        Assert.Equal((string?)"mocked_jwt_token", okHttpResult.Value?.AccessToken);
    }

    private void SetHeadersAndForm(
        Dictionary<string, StringValues> form,
        Dictionary<string, string> headers
    )
    {
        var formCollection = new FormCollection(form);
        _sut.HttpContext.Request.Form = formCollection;

        foreach (var headerKvp in headers)
        {
            _sut.HttpContext.Request.Headers[headerKvp.Key] = headerKvp.Value;
        }
    }
}
