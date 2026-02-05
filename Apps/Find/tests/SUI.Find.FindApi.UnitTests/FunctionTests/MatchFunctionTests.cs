using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using OneOf.Types;
using SUI.Find.Application.Enums.Matching;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Models.Matching;
using SUI.Find.Application.Services;
using SUI.Find.FindApi.Configurations;
using SUI.Find.FindApi.Functions.HttpFunctions;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.UnitTests.Mocks;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class MatchFunctionTests
{
    private const string TestApiKey = "test-api-key";
    private readonly ILogger<MatchFunction> _logger = Substitute.For<ILogger<MatchFunction>>();
    private readonly IMatchPersonOrchestrationService _matchPersonOrchestrationService =
        Substitute.For<IMatchPersonOrchestrationService>();
    private readonly IOptions<MatchFunctionConfiguration> _matchFunctionConfig;

    public MatchFunctionTests()
    {
        _matchFunctionConfig = Substitute.For<IOptions<MatchFunctionConfiguration>>();
        _matchFunctionConfig.Value.Returns(new MatchFunctionConfiguration { XApiKey = TestApiKey });
    }

    private MatchFunction CreateFunction() =>
        new(_logger, _matchPersonOrchestrationService, _matchFunctionConfig);

    private static FunctionContext CreateContextWithAuth(string clientId = "test-client-id")
    {
        var context = Substitute.For<FunctionContext>();
        context.Items.Returns(
            new Dictionary<object, object> { { "AuthContext", new AuthContext(clientId, []) } }
        );
        context.InvocationId.Returns(Guid.NewGuid().ToString());
        return context;
    }

    private static HttpHeadersCollection CreateHeadersWithApiKey(string? apiKey = TestApiKey)
    {
        var headers = new HttpHeadersCollection();
        if (apiKey != null)
        {
            headers.Add("x-api-key", new[] { apiKey });
        }
        return headers;
    }

    [Fact]
    public async Task ShouldReturnOk_WithEncryptedSuid_WhenMatchIsSuccessful()
    {
        // Arrange
        var function = CreateFunction();
        var context = CreateContextWithAuth();
        var validRequest = new MatchPersonRequest
        {
            Given = "John",
            Family = "Doe",
            BirthDate = DateOnly.Parse("1990-01-01"),
        };
        var headers = CreateHeadersWithApiKey();
        var req = MockHttpRequestData.CreateJson(validRequest, headers: headers);
        var encryptedPersonId = new EncryptedSuidPersonId("some-encrypted-id");
        _matchPersonOrchestrationService
            .FindPersonIdAsync(
                Arg.Any<PersonSpecification>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(encryptedPersonId);

        // Act
        var response = await function.MatchPerson(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response.Body.Position = 0;
        var responseBody = await JsonSerializer.DeserializeAsync<PersonMatch>(response.Body);
        Assert.NotNull(responseBody);
        Assert.Equal(encryptedPersonId.Value, responseBody.PersonId);
    }

    [Fact]
    public async Task ShouldReturnNotFound_WhenNoMatchFound()
    {
        // Arrange
        var function = CreateFunction();
        var context = CreateContextWithAuth();
        var validRequest = new MatchPersonRequest
        {
            Given = "John",
            Family = "Doe",
            BirthDate = DateOnly.Parse("1990-01-01"),
        };
        var headers = CreateHeadersWithApiKey();
        var req = MockHttpRequestData.CreateJson(validRequest, headers: headers);
        _matchPersonOrchestrationService
            .FindPersonIdAsync(
                Arg.Any<PersonSpecification>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new NotFound());

        // Act
        var response = await function.MatchPerson(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShouldProblem_WhenErrorOccurs()
    {
        // Arrange
        var function = CreateFunction();
        var context = CreateContextWithAuth();
        var validRequest = new MatchPersonRequest
        {
            Given = "John",
            Family = "Doe",
            BirthDate = DateOnly.Parse("1990-01-01"),
        };
        var headers = CreateHeadersWithApiKey();
        var req = MockHttpRequestData.CreateJson(validRequest, headers: headers);
        _matchPersonOrchestrationService
            .FindPersonIdAsync(
                Arg.Any<PersonSpecification>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new Error());

        // Act
        var response = await function.MatchPerson(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnUnauthorized_WhenAuthContextMissing()
    {
        // Arrange
        var function = CreateFunction();
        var context = Substitute.For<FunctionContext>();
        context.Items.Returns(new Dictionary<object, object>());
        context.InvocationId.Returns(Guid.NewGuid().ToString());
        var validRequest = new MatchPersonRequest
        {
            Given = "John",
            Family = "Doe",
            BirthDate = DateOnly.Parse("1990-01-01"),
        };
        var headers = CreateHeadersWithApiKey();
        var req = MockHttpRequestData.CreateJson(validRequest, headers: headers);

        // Act
        var response = await function.MatchPerson(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnUnauthorized_WhenApiKeyMissing()
    {
        // Arrange
        var function = CreateFunction();
        var context = CreateContextWithAuth();
        var validRequest = new MatchPersonRequest
        {
            Given = "John",
            Family = "Doe",
            BirthDate = DateOnly.Parse("1990-01-01"),
        };
        var headers = CreateHeadersWithApiKey(null);
        var req = MockHttpRequestData.CreateJson(validRequest, headers: headers);

        // Act
        var response = await function.MatchPerson(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnUnauthorized_WhenApiKeyIsInvalid()
    {
        // Arrange
        var function = CreateFunction();
        var context = CreateContextWithAuth();
        var validRequest = new MatchPersonRequest
        {
            Given = "John",
            Family = "Doe",
            BirthDate = DateOnly.Parse("1990-01-01"),
        };
        var headers = CreateHeadersWithApiKey("wrong-api-key");
        var req = MockHttpRequestData.CreateJson(validRequest, headers: headers);

        // Act
        var response = await function.MatchPerson(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnUnauthorized_WhenApiKeyIsEmpty()
    {
        // Arrange
        var function = CreateFunction();
        var context = CreateContextWithAuth();
        var validRequest = new MatchPersonRequest
        {
            Given = "John",
            Family = "Doe",
            BirthDate = DateOnly.Parse("1990-01-01"),
        };
        var headers = CreateHeadersWithApiKey("");
        var req = MockHttpRequestData.CreateJson(validRequest, headers: headers);

        // Act
        var response = await function.MatchPerson(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnBadRequest_WhenServiceReturnsDataQualityResult()
    {
        // Arrange
        var function = CreateFunction();
        var context = CreateContextWithAuth();
        var inValidRequest = new PersonSpecification
        {
            Given = "John",
            Family = "Doe but it doesnt matter because were returning an invalid request anyway",
            BirthDate = DateOnly.Parse("1990-01-01"),
        };
        _matchPersonOrchestrationService
            .FindPersonIdAsync(
                Arg.Any<PersonSpecification>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new DataQualityResult() { Given = QualityType.Invalid });

        context.InvocationId.Returns(Guid.NewGuid().ToString());

        var headers = CreateHeadersWithApiKey();
        var req = MockHttpRequestData.CreateJson(inValidRequest, headers: headers);

        // Act
        var response = await function.MatchPerson(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnBadRequest_WhenRequestIsMissingBody()
    {
        // Edge case test for null body

        // Arrange
        var service = Substitute.For<IMatchPersonOrchestrationService>();
        var logger = Substitute.For<ILogger<MatchFunction>>();
        var config = Substitute.For<IOptions<MatchFunctionConfiguration>>();
        config.Value.Returns(new MatchFunctionConfiguration { XApiKey = TestApiKey });
        var function = new MatchFunction(logger, service, config);

        var context = CreateContextWithAuth();
        context.InvocationId.Returns(Guid.NewGuid().ToString());

        var headers = CreateHeadersWithApiKey();
        var req = MockHttpRequestData.Create(requestData: null!, headers: headers);

        // Act
        var response = await function.MatchPerson(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnBadRequest_WhenJsonSerializerThrowsFromUserInput()
    {
        // Arrange
        var service = Substitute.For<IMatchPersonOrchestrationService>();
        var logger = Substitute.For<ILogger<MatchFunction>>();
        var config = Substitute.For<IOptions<MatchFunctionConfiguration>>();
        config.Value.Returns(new MatchFunctionConfiguration { XApiKey = TestApiKey });
        var function = new MatchFunction(logger, service, config);

        var context = CreateContextWithAuth();
        context.InvocationId.Returns(Guid.NewGuid().ToString());

        var headers = CreateHeadersWithApiKey();
        var req = MockHttpRequestData.Create(requestData: "", headers: headers);

        // Act
        var response = await function.MatchPerson(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
