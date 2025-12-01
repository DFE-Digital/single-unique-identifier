using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Models;
using SUI.Find.Application.Services;
using SUI.Find.FindApi.Functions.HttpTriggers;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.UnitTests.Mocks;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class CancelSearchFunctionTests
{
    private readonly CancelSearchFunction _sut;
    private readonly ISearchService _searchService = Substitute.For<ISearchService>();
    private readonly DurableTaskClient _client = Substitute.For<DurableTaskClient>("name");
    private readonly FunctionContext _context = Substitute.For<FunctionContext>();
    private const string JobId = "test-job-id";
    private const string ClientId = "test-client-id";

    public CancelSearchFunctionTests()
    {
        _sut = new CancelSearchFunction(
            Substitute.For<ILogger<CancelSearchFunction>>(),
            _searchService
        );

        // Setup AuthContext in FunctionContext.Items
        var items = new Dictionary<object, object>
        {
            [ApplicationConstants.Auth.AuthContextKey] = new AuthContext(ClientId, []),
        };
        _context.Items.Returns(items);
    }

    [Fact]
    public async Task ShouldReturnOkResponse_WhenCancellationIsSuccessful()
    {
        // Arrange
        var httpRequestData = MockHttpRequestData.Create(
            new Dictionary<string, StringValues> { { "jobId", JobId } }
        );
        _searchService
            .CancelSearchAsync(JobId, ClientId, _client, CancellationToken.None)
            .Returns(
                new SearchCancelResult.Success(
                    new SearchJobDto
                    {
                        JobId = JobId,
                        Suid = "test-suid",
                        Status = SearchStatus.Cancelled,
                        CreatedAt = DateTime.UtcNow,
                        LastUpdatedAt = default,
                    }
                )
            );

        // Act
        var result = await _sut.CancelSearch(
            httpRequestData,
            _client,
            JobId,
            _context,
            CancellationToken.None
        );

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Accepted, result.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnNotFoundResponse_WhenJobDoesNotExist()
    {
        // Arrange
        var httpRequestData = MockHttpRequestData.Create(
            new Dictionary<string, StringValues> { { "jobId", JobId } }
        );
        _searchService
            .CancelSearchAsync(JobId, ClientId, _client, CancellationToken.None)
            .Returns(new SearchCancelResult.NotFound());

        // Act
        var result = await _sut.CancelSearch(
            httpRequestData,
            _client,
            JobId,
            _context,
            CancellationToken.None
        );

        // Assert
        result.Body.Position = 0;
        var problemResponse = await JsonSerializer.DeserializeAsync<Problem>(result.Body);
        Assert.Equal((int)System.Net.HttpStatusCode.NotFound, problemResponse!.Status);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnSuccess_WhenJobIsInNonCancellableState()
    {
        // Arrange
        var httpRequestData = MockHttpRequestData.Create(
            new Dictionary<string, StringValues> { { "jobId", JobId } }
        );
        _searchService
            .CancelSearchAsync(JobId, ClientId, _client, CancellationToken.None)
            .Returns(
                new SearchCancelResult.Success(
                    new SearchJobDto
                    {
                        JobId = JobId,
                        Suid = "test-suid",
                        Status = SearchStatus.Cancelled,
                        CreatedAt = DateTime.UtcNow,
                        LastUpdatedAt = default,
                    }
                )
            );

        // Act
        var result = await _sut.CancelSearch(
            httpRequestData,
            _client,
            JobId,
            _context,
            CancellationToken.None
        );

        // Assert
        result.Body.Position = 0;
        var response = await JsonSerializer.DeserializeAsync<SearchJob>(result.Body);
        Assert.Equal(System.Net.HttpStatusCode.Accepted, result.StatusCode);
        Assert.Equal(JobId, response!.JobId);
        Assert.Equal("test-suid", response.Suid);
    }

    [Fact]
    public async Task ShouldReturnFailedResponse_WhenCancellationFails()
    {
        // Arrange
        var httpRequestData = MockHttpRequestData.Create(
            new Dictionary<string, StringValues> { { "jobId", JobId } }
        );
        _searchService
            .CancelSearchAsync(JobId, ClientId, _client, CancellationToken.None)
            .Returns(new SearchCancelResult.Error());

        // Act
        var result = await _sut.CancelSearch(
            httpRequestData,
            _client,
            JobId,
            _context,
            CancellationToken.None
        );

        // Assert
        result.Body.Position = 0;
        var problemResponse = await JsonSerializer.DeserializeAsync<Problem>(result.Body);
        Assert.Equal((int)System.Net.HttpStatusCode.InternalServerError, problemResponse!.Status);
        Assert.Equal(System.Net.HttpStatusCode.InternalServerError, result.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedResponse_WhenAuthContextIsMissing()
    {
        // Arrange
        var httpRequestData = MockHttpRequestData.Create(
            new Dictionary<string, StringValues> { { "jobId", JobId } }
        );
        var contextWithoutAuth = Substitute.For<FunctionContext>();
        contextWithoutAuth.Items.Returns(new Dictionary<object, object>());

        // Act
        var result = await _sut.CancelSearch(
            httpRequestData,
            _client,
            JobId,
            contextWithoutAuth,
            CancellationToken.None
        );

        // Assert
        result.Body.Position = 0;
        var problemResponse = await JsonSerializer.DeserializeAsync<Problem>(result.Body);
        Assert.Equal((int)System.Net.HttpStatusCode.Unauthorized, problemResponse!.Status);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, result.StatusCode);
    }
}
