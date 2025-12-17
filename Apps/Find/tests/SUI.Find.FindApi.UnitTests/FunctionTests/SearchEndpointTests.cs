using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OneOf.Types;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Models;
using SUI.Find.Application.Services;
using SUI.Find.Domain.ValueObjects;
using SUI.Find.FindApi.Functions.HttpFunctions;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.UnitTests.Mocks;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class SearchEndpointTests
{
    private readonly SearchFunction _sut;
    private readonly DurableTaskClient _client = Substitute.For<DurableTaskClient>("name");
    private readonly FunctionContext _context = Substitute.For<FunctionContext>();
    private readonly ISearchService _searchService = Substitute.For<ISearchService>();

    private const string ValidSuid = "Cy13hyZL-4LSIwVy50p-Hg";
    private const string InvalidSuid = "invalid-suid";
    private const string TestClientId = "test-client-id";

    public SearchEndpointTests()
    {
        _context.InvocationId.Returns(Guid.NewGuid().ToString());
        var items = new Dictionary<object, object>
        {
            [ApplicationConstants.Auth.AuthContextKey] = new AuthContext(TestClientId, []),
        };
        _context.Items.Returns(items);
        var logger = Substitute.For<ILogger<SearchFunction>>();
        _sut = new SearchFunction(logger, _searchService);
    }

    [Fact]
    public async Task ShouldReturn401_WhenAuthContextMissing()
    {
        var items = new Dictionary<object, object>();
        _context.Items.Returns(items);
        var data = new StartSearchRequest(ValidSuid);
        var httpRequestData = MockHttpRequestData.CreateJson(data);

        var response = await _sut.Searches(
            httpRequestData,
            _client,
            _context,
            CancellationToken.None
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenRequestSuidIsInvalid()
    {
        var data = new StartSearchRequest(InvalidSuid);
        var httpRequestData = MockHttpRequestData.CreateJson(data);

        var response = await _sut.Searches(
            httpRequestData,
            _client,
            _context,
            CancellationToken.None
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn202_WithSearchJobData_WhenSuccessful()
    {
        var data = new StartSearchRequest(ValidSuid);
        var httpRequestData = MockHttpRequestData.CreateJson(data);

        var jobDto = new SearchJobDto
        {
            JobId = "job123",
            PersonId = ValidSuid,
            Status = SearchStatus.Queued,
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow,
        };
        _searchService
            .StartSearchAsync(
                Arg.Any<EncryptedPersonId>(),
                Arg.Any<string>(),
                Arg.Any<string[]>(),
                Arg.Any<DurableTaskClient>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(jobDto);

        var response = await _sut.Searches(
            httpRequestData,
            _client,
            _context,
            CancellationToken.None
        );

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        response.Body.Position = 0;
        var returnedJob = await JsonSerializer.DeserializeAsync<SearchJob>(
            response.Body,
            JsonSerializerOptions.Web
        );
        Assert.NotNull(returnedJob);
        Assert.Equal("job123", returnedJob.JobId);
        Assert.Equal(ValidSuid, returnedJob.Suid);
        Assert.Equal(SearchStatus.Queued, returnedJob.Status);
    }

    [Fact]
    public async Task ShouldReturn500_WhenSearchServiceReturnsFailed()
    {
        var data = new StartSearchRequest(ValidSuid);
        var httpRequestData = MockHttpRequestData.CreateJson(data);

        _searchService
            .StartSearchAsync(
                Arg.Any<EncryptedPersonId>(),
                Arg.Any<string>(),
                Arg.Any<string[]>(),
                Arg.Any<DurableTaskClient>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new Error());

        var response = await _sut.Searches(
            httpRequestData,
            _client,
            _context,
            CancellationToken.None
        );

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
