using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.Find.Domain.Models;
using SUI.Find.FindApi.Functions.HttpFunctions;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.Models.Auth;
using SUI.Find.FindApi.UnitTests.Mocks;
using SUI.Find.Infrastructure.Models;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class AuthTokenFunctionTests
{
    private readonly AuthTokenFunction _sut;
    private readonly FunctionContext _context = Substitute.For<FunctionContext>();
    private readonly IAuthStoreService _authStoreService = Substitute.For<IAuthStoreService>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();

    public AuthTokenFunctionTests()
    {
        var logger = Substitute.For<ILogger<AuthTokenFunction>>();
        _sut = new AuthTokenFunction(logger, _authStoreService, _jwtTokenService);
    }

    [Fact]
    public async Task ShouldReturnProblemResponse_WhenNoFormDetailsProvided()
    {
        // Arrange
        var httpRequestData = MockHttpRequestData.Create();

        // Act
        var result = await _sut.AuthToken(httpRequestData, _context);

        // Assert
        result.Body.Position = 0;
        var responseData = await JsonSerializer.DeserializeAsync<Problem>(result.Body);
        Assert.NotNull(responseData?.Title);
    }

    [Fact]
    public async Task ShouldReturnProblemResponse_WhenNoAuthorizationHeaderProvided()
    {
        // Arrange
        var httpRequestData = MockHttpRequestData.CreateFormData(
            new Dictionary<string, string> { { "grant_type", "client_credentials" } }
        );
        httpRequestData.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

        // Act
        var result = await _sut.AuthToken(httpRequestData, _context);

        // Assert
        result.Body.Position = 0;
        var responseData = await JsonSerializer.DeserializeAsync<Problem>(result.Body);
        Assert.NotNull(responseData?.Title);
    }

    [Fact]
    public async Task ShouldReturnProblemResponse_WhenInvalidContentTypeProvided()
    {
        // Arrange
        var httpRequestData = MockHttpRequestData.Create();
        httpRequestData.Headers.Add("Content-Type", "application/json");

        // Act
        var result = await _sut.AuthToken(httpRequestData, _context);

        // Assert
        result.Body.Position = 0;
        var responseData = await JsonSerializer.DeserializeAsync<Problem>(result.Body);
        Assert.NotNull(responseData?.Title);
    }

    [Fact]
    public async Task ShouldReturnProblemResponse_WhenNoClientIdOrSecretProvided()
    {
        // Arrange
        var httpRequestData = MockHttpRequestData.Create();
        httpRequestData.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

        // Act
        var result = await _sut.AuthToken(httpRequestData, _context);

        // Assert
        result.Body.Position = 0;
        var responseData = await JsonSerializer.DeserializeAsync<Problem>(result.Body);
        Assert.NotNull(responseData?.Title);
    }

    [Fact]
    public async Task ShouldReturnProblemResponse_WhenClientSecretIsMissing()
    {
        // Arrange
        // Only clientId, no clientSecret
        const string credentials = "valid_client_id:";
        var base64Credentials = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(credentials)
        );
        var httpRequestData = MockHttpRequestData.CreateFormData(
            new Dictionary<string, string> { { "grant_type", "client_credentials" } }
        );
        httpRequestData.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
        httpRequestData.Headers.Add("Authorization", $"Basic {base64Credentials}");

        // Act
        var result = await _sut.AuthToken(httpRequestData, _context);

        // Assert
        result.Body.Position = 0;
        var responseData = await JsonSerializer.DeserializeAsync<Problem>(result.Body);
        Assert.NotNull(responseData?.Title);
        Assert.Equal("Unauthorized", responseData.Title);
    }

    [Fact]
    public async Task ShouldThrowException_WhenFileIsNotFound()
    {
        // Arrange
        var httpRequestData = MockHttpRequestData.CreateFormData(
            new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "scope", "file.read" },
            }
        );
        httpRequestData.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
        httpRequestData.Headers.Add(
            "Authorization",
            "Basic " + Convert.ToBase64String("valid_client_id:valid_client_secret"u8.ToArray())
        );
        _authStoreService
            .GetClientByCredentials("valid_client_id", "valid_client_secret")
            .Throws(new InvalidOperationException("Auth store file not found at: some/path"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _sut.AuthToken(httpRequestData, _context);
        });
    }

    [Fact]
    public async Task ShouldReturnProblem_WhenInvalidScopeProvided()
    {
        // Arrange
        var httpRequestData = MockHttpRequestData.CreateFormData(
            new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "scope", "invalid.scope" },
            }
        );
        httpRequestData.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
        httpRequestData.Headers.Add(
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
        var result = await _sut.AuthToken(httpRequestData, _context);

        // Assert
        result.Body.Position = 0;
        var responseData = await JsonSerializer.DeserializeAsync<Problem>(result.Body);
        Assert.NotNull(responseData?.Title);
    }

    [Fact]
    public async Task ShouldReturnJwtTokenInResponse_WhenValidClientCredentialsProvided()
    {
        // Arrange
        var httpRequestData = MockHttpRequestData.CreateFormData(
            new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "scope", "file.read" },
            }
        );
        httpRequestData.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
        httpRequestData.Headers.Add(
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
        var result = await _sut.AuthToken(httpRequestData, _context);

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        result.Body.Position = 0;
        var responseData = await JsonSerializer.DeserializeAsync<AuthTokenResponse>(result.Body);
        Assert.NotNull(responseData);
        Assert.Equal("mocked_jwt_token", responseData?.AccessToken);
    }
}
