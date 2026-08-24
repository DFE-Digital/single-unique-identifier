using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OneOf.Types;
using SUI.GetAnIdentifier.API.Configuration;
using SUI.GetAnIdentifier.API.Functions;
using SUI.GetAnIdentifier.API.Models;
using SUI.GetAnIdentifier.API.UnitTests.Mocks;
using SUI.GetAnIdentifier.Application.Enum;
using SUI.GetAnIdentifier.Application.Interfaces;
using SUI.GetAnIdentifier.Application.Models;

namespace SUI.GetAnIdentifier.API.UnitTests.FunctionTests;

public class GetAnIdentifierTests
{
    private const string TestApiKey = "test-api-key";
    private readonly ILogger<GetAnIdentifierFunction> _logger = Substitute.For<
        ILogger<GetAnIdentifierFunction>
    >();
    private readonly IGetAnIdentifierService _getAnIdentifierService =
        Substitute.For<IGetAnIdentifierService>();
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly IOptions<GetAnIdentifierConfiguration> _matchFunctionConfig;

    public GetAnIdentifierTests()
    {
        _matchFunctionConfig = Substitute.For<IOptions<GetAnIdentifierConfiguration>>();
        _matchFunctionConfig.Value.Returns(
            new GetAnIdentifierConfiguration() { XApiKey = TestApiKey }
        );
    }

    private GetAnIdentifierFunction CreateFunction() =>
        new(_logger, _getAnIdentifierService, _auditLogService, _matchFunctionConfig);

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
        const string generalPractitionerOdsCode = "B81606";

        _getAnIdentifierService
            .MatchPersonAsync(
                Arg.Any<PersonSpecification>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new GetAnIdentifierResult(
                    NhsPersonId.Create(personId).Value!,
                    [generalPractitionerOdsCode]
                )
            );

        // Act
        var response = await function.GetAnIdentifier(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response.Body.Position = 0;
        using var responseBody = await JsonDocument.ParseAsync(response.Body);
        Assert.Equal(personId, responseBody.RootElement.GetProperty("PersonId").GetString());
        Assert.Equal(
            generalPractitionerOdsCode,
            responseBody
                .RootElement.GetProperty("GeneralPractitioner")
                .EnumerateArray()
                .Single()
                .GetString()
        );

        // Verify incoming and outgoing audit logs were written
        await _auditLogService
            .Received(1)
            .LogIncomingRequestAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<object?>(),
                Arg.Any<CancellationToken>()
            );

        await _auditLogService
            .Received(1)
            .LogOutgoingResponseAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<DateTimeOffset>(),
                (int)HttpStatusCode.OK,
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            );
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
            .MatchPersonAsync(
                Arg.Any<PersonSpecification>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new NotFound());

        // Act
        var response = await function.GetAnIdentifier(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnBadGateway_WhenDownstreamErrorOccurs()
    {
        // Arrange
        var function = CreateFunction();
        var context = CreateContextWithAuth();
        var validRequest = CreateMatchRequest();
        var headers = CreateHeadersWithApiKey();
        var req = MockHttpRequestData.CreateJson(validRequest, headers: headers);

        _getAnIdentifierService
            .MatchPersonAsync(
                Arg.Any<PersonSpecification>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new Error()); // Simulate the failure from the FHIR service

        // Act
        var response = await function.GetAnIdentifier(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnInternalServerError_WhenUnhandledExceptionOccurs()
    {
        // Arrange
        var function = CreateFunction();
        var context = CreateContextWithAuth();
        var validRequest = CreateMatchRequest();
        var headers = CreateHeadersWithApiKey();
        var req = MockHttpRequestData.CreateJson(validRequest, headers: headers);

        _getAnIdentifierService
            .MatchPersonAsync(
                Arg.Any<PersonSpecification>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()
            )
            .ThrowsAsync(new Exception("Unexpected system crash")); // Simulate an unhandled code crash

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
            .MatchPersonAsync(
                Arg.Any<PersonSpecification>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>()
            )
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
        // Arrange
        var service = Substitute.For<IGetAnIdentifierService>();
        var logger = Substitute.For<ILogger<GetAnIdentifierFunction>>();
        var auditLogger = Substitute.For<IAuditLogService>();
        var config = Substitute.For<IOptions<GetAnIdentifierConfiguration>>();
        config.Value.Returns(new GetAnIdentifierConfiguration() { XApiKey = TestApiKey });
        var function = new GetAnIdentifierFunction(logger, service, auditLogger, config);

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
        var auditLogger = Substitute.For<IAuditLogService>();
        var config = Substitute.For<IOptions<GetAnIdentifierConfiguration>>();
        config.Value.Returns(new GetAnIdentifierConfiguration() { XApiKey = TestApiKey });
        var function = new GetAnIdentifierFunction(logger, service, auditLogger, config);

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
