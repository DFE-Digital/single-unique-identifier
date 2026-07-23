using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using OneOf.Types;
using SUI.GetAnIdentifier.Application.Enum;
using SUI.GetAnIdentifier.Application.Interfaces;
using SUI.GetAnIdentifier.Application.Models;
using SUI.GetAnIdentifier.Function.Configuration;
using SUI.GetAnIdentifier.Function.Functions;
using SUI.GetAnIdentifier.Function.Models;
using SUI.GetAnIdentifier.Function.UnitTests.Mocks;

namespace SUI.GetAnIdentifier.Function.UnitTests.FunctionTests;

public class GetAnIdentifierTests
{
    private const string TestApiKey = "test-api-key";
    private readonly ILogger<GetAnIdentifierFunction> _logger = Substitute.For<
        ILogger<GetAnIdentifierFunction>
    >();
    private readonly IGetAnIdentifierService _getAnIdentifierService =
        Substitute.For<IGetAnIdentifierService>();
    private readonly IOptions<GetAnIdentifierConfiguration> _matchFunctionConfig;

    public GetAnIdentifierTests()
    {
        _matchFunctionConfig = Substitute.For<IOptions<GetAnIdentifierConfiguration>>();
        _matchFunctionConfig.Value.Returns(
            new GetAnIdentifierConfiguration() { XApiKey = TestApiKey }
        );
    }

    private GetAnIdentifierFunction CreateFunction() =>
        new(_logger, _getAnIdentifierService, _matchFunctionConfig);

    private static FunctionContext CreateContextWithAuth(string organisationId = "test-org-id")
    {
        var context = Substitute.For<FunctionContext>();
        context.Items.Returns(
            new Dictionary<object, object>
            {
                { "AuthContext", new AuthContext(Guid.NewGuid().ToString(), organisationId, []) },
            }
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

    private static GetAnIdentifierRequest CreateMatchRequest() =>
        new()
        {
            PersonSpecification = new PersonSpecification
            {
                Given = "John",
                Family = "Doe",
                BirthDate = DateOnly.Parse("1990-01-01"),
            },
            Metadata =
            [
                new GetAnIdentifierRequestMetadata
                {
                    RecordType = "Test RecordType",
                    SystemId = "Test System",
                    RecordId = "9999999999",
                },
            ],
        };

    [Fact]
    public async Task ShouldReturnOk_WithSuid_WhenMatchIsSuccessful()
    {
        // Arrange
        var function = CreateFunction();
        var context = CreateContextWithAuth();
        var validRequest = CreateMatchRequest();
        var headers = CreateHeadersWithApiKey();
        var req = MockHttpRequestData.CreateJson(validRequest, headers: headers);
        var personId = "9876543210";
        _getAnIdentifierService
            .MatchPersonAsync(Arg.Any<PersonSpecification>(), Arg.Any<CancellationToken>())
            .Returns(NhsPersonId.Create(personId).Value!);

        // Act
        var response = await function.GetAnIdentifier(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response.Body.Position = 0;
        var responseBody = await JsonSerializer.DeserializeAsync<PersonMatch>(response.Body);
        Assert.NotNull(responseBody);
        Assert.Equal(personId, responseBody.PersonId);
    }

    [Fact]
    public async Task ShouldReturnNotFound_WhenNoMatchFound()
    {
        // Arrange
        var function = CreateFunction();
        var context = CreateContextWithAuth();
        var validRequest = CreateMatchRequest();
        var headers = CreateHeadersWithApiKey();
        var req = MockHttpRequestData.CreateJson(validRequest, headers: headers);
        _getAnIdentifierService
            .MatchPersonAsync(Arg.Any<PersonSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new NotFound());

        // Act
        var response = await function.GetAnIdentifier(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShouldProblem_WhenErrorOccurs()
    {
        // Arrange
        var function = CreateFunction();
        var context = CreateContextWithAuth();
        var validRequest = CreateMatchRequest();
        var headers = CreateHeadersWithApiKey();
        var req = MockHttpRequestData.CreateJson(validRequest, headers: headers);
        _getAnIdentifierService
            .MatchPersonAsync(Arg.Any<PersonSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new Error());

        // Act
        var response = await function.GetAnIdentifier(req, context, CancellationToken.None);

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
        var validRequest = CreateMatchRequest();
        var headers = CreateHeadersWithApiKey();
        var req = MockHttpRequestData.CreateJson(validRequest, headers: headers);

        // Act
        var response = await function.GetAnIdentifier(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnUnauthorized_WhenApiKeyMissing()
    {
        // Arrange
        var function = CreateFunction();
        var context = CreateContextWithAuth();
        var validRequest = CreateMatchRequest();
        var headers = CreateHeadersWithApiKey(null);
        var req = MockHttpRequestData.CreateJson(validRequest, headers: headers);

        // Act
        var response = await function.GetAnIdentifier(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnUnauthorized_WhenApiKeyIsInvalid()
    {
        // Arrange
        var function = CreateFunction();
        var context = CreateContextWithAuth();
        var validRequest = CreateMatchRequest();
        var headers = CreateHeadersWithApiKey("wrong-api-key");
        var req = MockHttpRequestData.CreateJson(validRequest, headers: headers);

        // Act
        var response = await function.GetAnIdentifier(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnUnauthorized_WhenApiKeyIsEmpty()
    {
        // Arrange
        var function = CreateFunction();
        var context = CreateContextWithAuth();
        var validRequest = CreateMatchRequest();
        var headers = CreateHeadersWithApiKey("");
        var req = MockHttpRequestData.CreateJson(validRequest, headers: headers);

        // Act
        var response = await function.GetAnIdentifier(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnBadRequest_WhenServiceReturnsDataQualityResult()
    {
        // Arrange
        var function = CreateFunction();
        var context = CreateContextWithAuth();
        var inValidRequest = CreateMatchRequest();
        _getAnIdentifierService
            .MatchPersonAsync(Arg.Any<PersonSpecification>(), Arg.Any<CancellationToken>())
            .Returns(new DataQualityResult() { Given = QualityType.Invalid });

        context.InvocationId.Returns(Guid.NewGuid().ToString());

        var headers = CreateHeadersWithApiKey();
        var req = MockHttpRequestData.CreateJson(inValidRequest, headers: headers);

        // Act
        var response = await function.GetAnIdentifier(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnBadRequest_WhenRequestIsMissingBody()
    {
        // Edge case test for null body

        // Arrange
        var service = Substitute.For<IGetAnIdentifierService>();
        var logger = Substitute.For<ILogger<GetAnIdentifierFunction>>();
        var config = Substitute.For<IOptions<GetAnIdentifierConfiguration>>();
        config.Value.Returns(new GetAnIdentifierConfiguration() { XApiKey = TestApiKey });
        var function = new GetAnIdentifierFunction(logger, service, config);

        var context = CreateContextWithAuth();
        context.InvocationId.Returns(Guid.NewGuid().ToString());

        var headers = CreateHeadersWithApiKey();
        var req = MockHttpRequestData.Create(requestData: null!, headers: headers);

        // Act
        var response = await function.GetAnIdentifier(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnBadRequest_WhenJsonSerializerThrowsFromUserInput()
    {
        // Arrange
        var service = Substitute.For<IGetAnIdentifierService>();
        var logger = Substitute.For<ILogger<GetAnIdentifierFunction>>();
        var config = Substitute.For<IOptions<GetAnIdentifierConfiguration>>();
        config.Value.Returns(new GetAnIdentifierConfiguration() { XApiKey = TestApiKey });
        var function = new GetAnIdentifierFunction(logger, service, config);

        var context = CreateContextWithAuth();
        context.InvocationId.Returns(Guid.NewGuid().ToString());

        var headers = CreateHeadersWithApiKey();
        var req = MockHttpRequestData.Create(requestData: "", headers: headers);

        // Act
        var response = await function.GetAnIdentifier(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
