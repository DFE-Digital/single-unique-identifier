using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Models;
using SUI.Find.Application.Services;
using SUI.Find.FindApi.Functions.HttpTriggers;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.UnitTests.Mocks;
using CancellationToken = System.Threading.CancellationToken;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class SearchStatusFunctionTests
{
    private readonly DurableTaskClient _client = Substitute.For<DurableTaskClient>("name");
    private static readonly ILogger<SearchStatusFunction> Logger = Substitute.For<
        ILogger<SearchStatusFunction>
    >();
    private readonly ISearchService _searchService = Substitute.For<ISearchService>();
    private readonly SearchStatusFunction _sut;

    public SearchStatusFunctionTests()
    {
        _sut = new SearchStatusFunction(Logger, _searchService);
    }

    private static FunctionContext CreateContextWithAuth(string clientId = "test-client-id")
    {
        var context = Substitute.For<FunctionContext>();
        context.Items.Returns(
            new Dictionary<object, object>
            {
                {
                    Application.Constants.ApplicationConstants.Auth.AuthContextKey,
                    new AuthContext(clientId, [])
                },
            }
        );
        context.InvocationId.Returns(Guid.NewGuid().ToString());
        return context;
    }

    [Fact]
    public async Task ShouldReturnUnauthorized_WhenClientIdHeaderIsMissing()
    {
        // Arrange
        var context = Substitute.For<FunctionContext>();
        context.Items.Returns(new Dictionary<object, object>());
        context.InvocationId.Returns(Guid.NewGuid().ToString());
        var req = MockHttpRequestData.Create();

        // Act
        var response = await _sut.SearchJobTrigger(
            req,
            _client,
            context,
            "job-1",
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnInternalServerError_WhenServiceReturnsFailure()
    {
        // Arrange
        var context = CreateContextWithAuth();
        var req = MockHttpRequestData.Create();
        _searchService
            .GetSearchStatusAsync("job-2", "test-client-id", _client, Arg.Any<CancellationToken>())
            .Returns(new SearchJobResult.Failed());

        // Act
        var response = await _sut.SearchJobTrigger(
            req,
            _client,
            context,
            "job-2",
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnNotFound_WhenJobDoesNotExist()
    {
        // Arrange
        var context = CreateContextWithAuth();
        var req = MockHttpRequestData.Create();
        _searchService
            .GetSearchStatusAsync("job-3", "test-client-id", _client, Arg.Any<CancellationToken>())
            .Returns(new SearchJobResult.NotFound());

        // Act
        var response = await _sut.SearchJobTrigger(
            req,
            _client,
            context,
            "job-3",
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnUnauthorized_WhenServiceReturnsUnauthorized()
    {
        // Arrange
        var context = CreateContextWithAuth();
        var req = MockHttpRequestData.Create();
        _searchService
            .GetSearchStatusAsync("job-4", "test-client-id", _client, Arg.Any<CancellationToken>())
            .Returns(new SearchJobResult.Unauthorized());

        // Act
        var response = await _sut.SearchJobTrigger(
            req,
            _client,
            context,
            "job-4",
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnOk_WhenJobExistsAndClientIdMatches()
    {
        // Arrange
        var context = CreateContextWithAuth();
        var req = MockHttpRequestData.Create();
        var dto = new SearchJobDto
        {
            JobId = "job-5",
            Suid = "SUI-123",
            Status = SearchStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
        };
        _searchService
            .GetSearchStatusAsync("job-5", "test-client-id", _client, Arg.Any<CancellationToken>())
            .Returns(new SearchJobResult.Success(dto));

        // Act
        var response = await _sut.SearchJobTrigger(
            req,
            _client,
            context,
            "job-5",
            CancellationToken.None
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
