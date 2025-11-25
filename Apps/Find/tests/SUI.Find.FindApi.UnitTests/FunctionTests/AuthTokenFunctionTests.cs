using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.FindApi.Functions.HttpTriggers;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.UnitTests.Mocks;
using SUI.Find.Infrastructure.Models;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class AuthTokenFunctionTests
{
    private readonly AuthTokenFunction _sut;
    private readonly FunctionContext _context = Substitute.For<FunctionContext>();
    private readonly IAuthStoreService _authStoreService = Substitute.For<IAuthStoreService>();

    public AuthTokenFunctionTests()
    {
        var logger = Substitute.For<ILogger<AuthTokenFunction>>();
        _sut = new AuthTokenFunction(logger, _authStoreService);
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
    public async Task ShouldReturnOkResponse_WhenValidClientCredentialsProvided()
    {
        // Arrange
        var httpRequestData = MockHttpRequestData.CreateFormData(
            new Dictionary<string, string> { { "grant_type", "client_credentials" } }
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
                    }
                )
            );

        // Act
        var result = await _sut.AuthToken(httpRequestData, _context);

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
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
}
