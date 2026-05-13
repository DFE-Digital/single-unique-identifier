using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using OneOf.Types;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Enums.Matching;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Models.Matching;
using SUI.Find.FindApi.Configurations;
using SUI.Find.FindApi.Functions.HttpFunctions;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.UnitTests.Mocks;
using SUI.Find.Infrastructure.Repositories.SuiCustodianRegister;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class MatchFunctionTests
{
    private const string TestApiKey = "test-api-key";
    private readonly ILogger<MatchFunction> _logger = Substitute.For<ILogger<MatchFunction>>();
    private readonly IMatchPersonOrchestrationService _matchPersonOrchestrationService =
        Substitute.For<IMatchPersonOrchestrationService>();
    private readonly IOptions<MatchFunctionConfiguration> _matchFunctionConfig;
    private readonly IIdRegisterRepository _idRegisterRepository =
        Substitute.For<IIdRegisterRepository>();

    public MatchFunctionTests()
    {
        _matchFunctionConfig = Substitute.For<IOptions<MatchFunctionConfiguration>>();
        _matchFunctionConfig.Value.Returns(new MatchFunctionConfiguration { XApiKey = TestApiKey });
    }

    private MatchFunction CreateFunction() =>
        new(_logger, _matchPersonOrchestrationService, _idRegisterRepository, _matchFunctionConfig);

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

    private static MatchRequest CreateMatchRequest() =>
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
                new MatchRequestMetadata
                {
                    RecordType = "Test RecordType",
                    SystemId = "Test System",
                    RecordId = "9999999999",
                },
            ],
        };

    [Fact]
    public async Task ShouldReturnOk_WithEncryptedSuid_WhenMatchIsSuccessful()
    {
        // Arrange
        var function = CreateFunction();
        var context = CreateContextWithAuth();
        var validRequest = CreateMatchRequest();
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
        await _idRegisterRepository
            .Received(1)
            .UpsertAsync(
                Arg.Is<IdRegisterEntry>(e =>
                    e.RecordType == "Test RecordType"
                    && e.CustodianSubjectId == "9999999999"
                    && e.SystemId == "Test System"
                    && e.CustodianId == "test-client-id"
                ),
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
        await _idRegisterRepository
            .DidNotReceive()
            .UpsertAsync(Arg.Any<IdRegisterEntry>(), Arg.Any<CancellationToken>());
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
        await _idRegisterRepository
            .DidNotReceive()
            .UpsertAsync(Arg.Any<IdRegisterEntry>(), Arg.Any<CancellationToken>());
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
        var response = await function.MatchPerson(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await _idRegisterRepository
            .DidNotReceive()
            .UpsertAsync(Arg.Any<IdRegisterEntry>(), Arg.Any<CancellationToken>());
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
        var response = await function.MatchPerson(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await _idRegisterRepository
            .DidNotReceive()
            .UpsertAsync(Arg.Any<IdRegisterEntry>(), Arg.Any<CancellationToken>());
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
        var response = await function.MatchPerson(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await _idRegisterRepository
            .DidNotReceive()
            .UpsertAsync(Arg.Any<IdRegisterEntry>(), Arg.Any<CancellationToken>());
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
        var response = await function.MatchPerson(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await _idRegisterRepository
            .DidNotReceive()
            .UpsertAsync(Arg.Any<IdRegisterEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnBadRequest_WhenServiceReturnsDataQualityResult()
    {
        // Arrange
        var function = CreateFunction();
        var context = CreateContextWithAuth();
        var inValidRequest = CreateMatchRequest();
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
        await _idRegisterRepository
            .DidNotReceive()
            .UpsertAsync(Arg.Any<IdRegisterEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnBadRequest_WhenRequestIsMissingBody()
    {
        // Edge case test for null body

        // Arrange
        var service = Substitute.For<IMatchPersonOrchestrationService>();
        var logger = Substitute.For<ILogger<MatchFunction>>();
        var repository = Substitute.For<IIdRegisterRepository>();
        var config = Substitute.For<IOptions<MatchFunctionConfiguration>>();
        config.Value.Returns(new MatchFunctionConfiguration { XApiKey = TestApiKey });
        var function = new MatchFunction(logger, service, repository, config);

        var context = CreateContextWithAuth();
        context.InvocationId.Returns(Guid.NewGuid().ToString());

        var headers = CreateHeadersWithApiKey();
        var req = MockHttpRequestData.Create(requestData: null!, headers: headers);

        // Act
        var response = await function.MatchPerson(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await _idRegisterRepository
            .DidNotReceive()
            .UpsertAsync(Arg.Any<IdRegisterEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturnBadRequest_WhenJsonSerializerThrowsFromUserInput()
    {
        // Arrange
        var service = Substitute.For<IMatchPersonOrchestrationService>();
        var logger = Substitute.For<ILogger<MatchFunction>>();
        var repository = Substitute.For<IIdRegisterRepository>();
        var config = Substitute.For<IOptions<MatchFunctionConfiguration>>();
        config.Value.Returns(new MatchFunctionConfiguration { XApiKey = TestApiKey });
        var function = new MatchFunction(logger, service, repository, config);

        var context = CreateContextWithAuth();
        context.InvocationId.Returns(Guid.NewGuid().ToString());

        var headers = CreateHeadersWithApiKey();
        var req = MockHttpRequestData.Create(requestData: "", headers: headers);

        // Act
        var response = await function.MatchPerson(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await _idRegisterRepository
            .DidNotReceive()
            .UpsertAsync(Arg.Any<IdRegisterEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldUseDefaultSystem_WhenMetadataSystemIdIsNull()
    {
        // Arrange
        var function = CreateFunction();
        var context = CreateContextWithAuth();

        var request = new MatchRequest
        {
            PersonSpecification = new PersonSpecification
            {
                Given = "John",
                Family = "Doe",
                BirthDate = DateOnly.Parse("1990-01-01"),
            },
            Metadata =
            [
                new MatchRequestMetadata
                {
                    RecordType = "Test RecordType",
                    SystemId = null!, // ← key scenario
                    RecordId = "9999999999",
                },
            ],
        };

        var headers = CreateHeadersWithApiKey();
        var req = MockHttpRequestData.CreateJson(request, headers: headers);

        var encryptedPersonId = new EncryptedSuidPersonId("some-encrypted-id");

        _matchPersonOrchestrationService
            .FindPersonIdAsync(
                Arg.Any<PersonSpecification>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(encryptedPersonId);

        // Act
        await function.MatchPerson(req, context, CancellationToken.None);

        // Assert
        await _idRegisterRepository
            .Received(1)
            .UpsertAsync(
                Arg.Is<IdRegisterEntry>(e => e.SystemId == ApplicationConstants.SystemIds.Default),
                Arg.Any<CancellationToken>()
            );
    }
}
