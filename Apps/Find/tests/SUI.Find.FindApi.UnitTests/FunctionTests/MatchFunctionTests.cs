using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OneOf.Types;
using SUI.Find.Application.Models;
using SUI.Find.Application.Services;
using SUI.Find.Domain.ValueObjects;
using SUI.Find.FindApi.Functions.HttpFunctions;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.UnitTests.Mocks;
using SUI.Find.Infrastructure.Models;
using Xunit;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class MatchFunctionTests
{
    private readonly ILogger<MatchFunction> _logger = Substitute.For<ILogger<MatchFunction>>();
    private readonly IMatchingService _service = Substitute.For<IMatchingService>();

    private MatchFunction CreateFunction() => new(_logger, _service);

    private static FunctionContext CreateContextWithAuth(string clientId = "test-client-id")
    {
        var context = Substitute.For<FunctionContext>();
        context.Items.Returns(
            new Dictionary<object, object> { { "AuthContext", new AuthContext(clientId, []) } }
        );
        context.InvocationId.Returns(Guid.NewGuid().ToString());
        return context;
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
        var req = MockHttpRequestData.CreateJson(validRequest);
        var encryptedPersonId = new EncryptedPersonId("encrypted-value");
        _service
            .MatchPersonAsync(Arg.Any<MatchPersonRequest>(), Arg.Any<string>())
            .Returns(encryptedPersonId);

        // Act
        var response = await function.MatchPerson(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response.Body.Position = 0;
        var responseBody = await JsonSerializer.DeserializeAsync<PersonMatch>(response.Body);
        Assert.NotNull(responseBody);
        Assert.Equal("encrypted-value", responseBody.PersonId);
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
        var req = MockHttpRequestData.CreateJson(validRequest);
        _service
            .MatchPersonAsync(Arg.Any<MatchPersonRequest>(), Arg.Any<string>())
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
        var req = MockHttpRequestData.CreateJson(validRequest);
        _service
            .MatchPersonAsync(Arg.Any<MatchPersonRequest>(), Arg.Any<string>())
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
        var req = MockHttpRequestData.CreateJson(validRequest);

        // Act
        var response = await function.MatchPerson(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnBadRequest_WhenRequestModelIsInvalid()
    {
        // Arrange
        var service = Substitute.For<IMatchingService>();
        var logger = Substitute.For<ILogger<MatchFunction>>();
        var function = new MatchFunction(logger, service);

        var context = CreateContextWithAuth();
        context.InvocationId.Returns(Guid.NewGuid().ToString());

        // Malformed request (empty body)
        var req = MockHttpRequestData.CreateJson("");

        // Act
        var response = await function.MatchPerson(req, context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
