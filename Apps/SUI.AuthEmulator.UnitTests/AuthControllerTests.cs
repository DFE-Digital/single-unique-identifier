using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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
        _sut = new AuthController(logger, _authStoreService, _jwtTokenService);
    }

    [Fact]
    public async Task ShouldReturnProblemResponse_WhenNoFormDetailsProvided()
    {
        // Arrange
        var httpRequestData = CreateJson("");

        // Act
        var result = await _sut.AuthToken(httpRequestData);

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
        var httpRequestData = CreateJson(
            "",
            query: new Dictionary<string, StringValues> { { "grant_type", "client_credentials" } }
        );
        httpRequestData.Headers.Append("Content-Type", "application/x-www-form-urlencoded");

        // Act
        var result = await _sut.AuthToken(httpRequestData);

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
    public async Task ShouldReturnProblemResponse_WhenInvalidContentTypeProvided()
    {
        // Arrange
        var httpRequestData = CreateJson(
            "",
            query: new Dictionary<string, StringValues> { { "grant_type", "client_credentials" } }
        );
        httpRequestData.Headers.Append("Content-Type", "application/json");

        // Act
        var result = await _sut.AuthToken(httpRequestData);

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
    public async Task ShouldReturnProblemResponse_WhenNoClientIdOrSecretProvided()
    {
        // Arrange
        var httpRequestData = CreateJson(
            "",
            query: new Dictionary<string, StringValues> { { "grant_type", "client_credentials" } }
        );
        httpRequestData.Headers.Append("Content-Type", "application/x-www-form-urlencoded");

        // Act
        var result = await _sut.AuthToken(httpRequestData);

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
        var httpRequestData = CreateJson(
            "",
            query: new Dictionary<string, StringValues> { { "grant_type", "client_credentials" } }
        );
        httpRequestData.Headers.Append("Content-Type", "application/x-www-form-urlencoded");
        httpRequestData.Headers.Append("Authorization", $"Basic {base64Credentials}");

        // Act
        var result = await _sut.AuthToken(httpRequestData);

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
        // Arrange
        var httpRequestData = CreateJson(
            "",
            new Dictionary<string, StringValues>
            {
                { "grant_type", "client_credentials" },
                { "scope", "file.read" },
            }
        );
        httpRequestData.Headers.Append("Content-Type", "application/x-www-form-urlencoded");
        httpRequestData.Headers.Append(
            "Authorization",
            "Basic " + Convert.ToBase64String("valid_client_id:valid_client_secret"u8.ToArray())
        );
        _authStoreService
            .GetClientByCredentials("valid_client_id", "valid_client_secret")
            .Throws(new InvalidOperationException("Auth store file not found at: some/path"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _sut.AuthToken(httpRequestData);
        });
    }

    [Fact]
    public async Task ShouldReturnProblem_WhenInvalidScopeProvided()
    {
        // Arrange
        var httpRequestData = CreateJson(
            "",
            new Dictionary<string, StringValues>
            {
                { "grant_type", "client_credentials" },
                { "scope", "invalid.scope" },
            }
        );
        httpRequestData.Headers.Append("Content-Type", "application/x-www-form-urlencoded");
        httpRequestData.Headers.Append(
            "Authorization",
            "Basic " + Convert.ToBase64String("valid_client_id:valid_client_secret"u8.ToArray())
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
        var result = await _sut.AuthToken(httpRequestData);

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
        var httpRequestData = CreateJson(
            "",
            new Dictionary<string, StringValues>
            {
                { "grant_type", "client_credentials" },
                { "scope", "file.read" },
            }
        );
        httpRequestData.Headers.Append("Content-Type", "application/x-www-form-urlencoded");
        httpRequestData.Headers.Append(
            "Authorization",
            "Basic " + Convert.ToBase64String("valid_client_id:valid_client_secret"u8.ToArray())
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
        var result = await _sut.AuthToken(httpRequestData);

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

    private static HttpRequest CreateJson<T>(
        T requestData,
        Dictionary<string, StringValues>? query = null,
        HeaderDictionary? headers = null
    )
        where T : class
    {
        var serializedData = JsonSerializer.Serialize(requestData);
        var bodyDataStream = new MemoryStream(Encoding.UTF8.GetBytes(serializedData));

        var queryDictionary = new Dictionary<string, StringValues>();
        if (query != null)
        {
            foreach (var key in query.Keys)
            {
                foreach (var value in query[key])
                {
                    queryDictionary.Add(key, value);
                }
            }
        }

        var queryCollection = new QueryCollection(queryDictionary);

        var request = Substitute.For<HttpRequest>();
        request.Body.Returns(bodyDataStream);
        request.Headers.Returns(headers ?? []);
        request.Query.Returns(queryCollection);

        return request;
    }

    private static HttpRequest CreateForm(
        Dictionary<string, string> requestData,
        Dictionary<string, StringValues>? query = null,
        HeaderDictionary? headers = null
    )
    {
        var formData = new FormUrlEncodedContent(requestData);
        var bodyDataStream = new MemoryStream();
        formData.CopyToAsync(bodyDataStream).Wait();
        bodyDataStream.Position = 0;

        var queryDictionary = new Dictionary<string, StringValues>();
        if (query != null)
        {
            foreach (var key in query.Keys)
            {
                foreach (var value in query[key])
                {
                    queryDictionary.Add(key, value);
                }
            }
        }

        var queryCollection = new QueryCollection(queryDictionary);

        var request = Substitute.For<HttpRequest>();
        request.Body.Returns(bodyDataStream);
        request.Headers.Returns(headers ?? []);
        request.Query.Returns(queryCollection);

        return request;
    }
}
