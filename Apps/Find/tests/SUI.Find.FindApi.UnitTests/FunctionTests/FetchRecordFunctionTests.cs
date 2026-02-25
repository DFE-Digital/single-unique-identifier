using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OneOf.Types;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.FindApi.Functions.HttpFunctions;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.UnitTests.Mocks;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class FetchRecordFunctionTests
{
    private readonly ILogger<FetchRecordFunction> _mockLogger = Substitute.For<
        ILogger<FetchRecordFunction>
    >();
    private readonly IFetchRecordService _mockService = Substitute.For<IFetchRecordService>();
    private readonly FetchRecordFunction _sut;

    public FetchRecordFunctionTests()
    {
        _sut = new FetchRecordFunction(_mockLogger, _mockService);
    }

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
    public async Task FetchRecord_ReturnsOk_WhenServiceSucceeds()
    {
        // Arrange
        var context = CreateContextWithAuth();
        var request = MockHttpRequestData.Create();

        var expectedResult = new CustodianRecord
        {
            RecordId = "record-123",
            RecordType = "record-type",
            Version = 1,
            PersonId = "person-456",
            SchemaUri = "schema-uri",
            Payload = JsonSerializer.Deserialize<JsonElement>("{}"),
            ContactDetails = new ContactDetails
            {
                Name = "contact-name",
                Email = "contact-email",
                Address = "contact-address",
                Telephone = "contact-telephone",
                Description = "contact-description",
            },
            RecordLink = new RecordLink { Title = "record-link-title", Url = "record-link-url" },
        };

        _mockService
            .FetchRecordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var response = await _sut.FetchRecord(
            request,
            "record-123",
            context,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response.Body.Position = 0;
        var responseBody = await JsonSerializer.DeserializeAsync<CustodianRecord>(response.Body);
        Assert.NotNull(responseBody);
        Assert.Equal(expectedResult.RecordId, responseBody.RecordId);
        Assert.Equal(expectedResult.RecordType, responseBody.RecordType);
        Assert.Equal(expectedResult.SchemaUri, responseBody.SchemaUri);
        Assert.Equal(expectedResult.PersonId, responseBody.PersonId);
        Assert.Equivalent(expectedResult.ContactDetails, responseBody.ContactDetails);
        Assert.Equivalent(expectedResult.RecordLink, responseBody.RecordLink);
    }

    [Fact]
    public async Task FetchRecord_ReturnsUnauthorized_WhenAuthContextMissing()
    {
        // Arrange
        var context = Substitute.For<FunctionContext>();
        context.Items.Returns(new Dictionary<object, object>());
        context.InvocationId.Returns(Guid.NewGuid().ToString());

        var request = MockHttpRequestData.Create();

        // Act
        var response = await _sut.FetchRecord(
            request,
            "record-123",
            context,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task FetchRecord_ReturnsBadRequest_WhenRecordIdIsEmpty()
    {
        // Arrange
        var context = CreateContextWithAuth();
        var request = MockHttpRequestData.Create();

        // Act
        var response = await _sut.FetchRecord(request, "", context, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FetchRecord_ReturnsNotFound_WhenServiceReturnsNotFound()
    {
        // Arrange
        var context = CreateContextWithAuth();
        var request = MockHttpRequestData.Create();

        _mockService
            .FetchRecordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Error());

        // Act
        var response = await _sut.FetchRecord(
            request,
            "record-123",
            context,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task FetchRecord_ReturnsNotFound_WhenServiceReturnsNotFoundErrors()
    {
        // Arrange
        var context = CreateContextWithAuth();
        var request = MockHttpRequestData.Create();

        _mockService
            .FetchRecordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new NotFound());

        // Act
        var response = await _sut.FetchRecord(
            request,
            "record-123",
            context,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnUnauthorized_WhenTheyAreNotTheRecordSearchOwner()
    {
        // Arrange
        var context = CreateContextWithAuth();
        var request = MockHttpRequestData.Create();

        _mockService
            .FetchRecordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Unauthorized());

        // Act
        var response = await _sut.FetchRecord(
            request,
            "record-123",
            context,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
