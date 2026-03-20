using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using OneOf.Types;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Models;
using SUI.Find.FindApi.Functions.HttpFunctions;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.UnitTests.Mocks;
using SUI.Find.Infrastructure.Interfaces;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class SearchResultsV2FunctionTests
{
    private readonly ILogger<SearchResultsV2Function> _logger = Substitute.For<
        ILogger<SearchResultsV2Function>
    >();

    private readonly IJobSearchService _jobSearchService = Substitute.For<IJobSearchService>();
    private readonly DurableTaskClient _client = Substitute.For<DurableTaskClient>("name");
    private readonly SearchResultsV2Function _sut;
    private const string WorkItemId = "test-work-item-id";

    private readonly HttpRequestData _httpRequestData = MockHttpRequestData.Create(
        new Dictionary<string, StringValues> { { "workItemId", WorkItemId } }
    );

    private readonly FunctionContext _authFunctionContext = CreateContextWithAuth();

    public SearchResultsV2FunctionTests()
    {
        _sut = new SearchResultsV2Function(_logger, _jobSearchService);
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

    private static SearchResultEntry CreateSearchResultEntry()
    {
        return new SearchResultEntry
        {
            RecordType = "Health",
            RecordUrl = "http://example.com/record/1",
            SystemId = "TestSystem",
            RecordId = "TestRecord",
            CustodianId = "20785465-0A26-4641-8859-AD4E341F88F0",
            CustodianName = "TestCustodian",
            JobId = "6109939B-44FC-4550-B0C4-D9BAC296A513",
            WorkItemId = "work-1",
        };
    }

    [Fact]
    public async Task ReturnsSuccess_WhenResultsFound()
    {
        var dto = new SearchResultsV2Dto
        {
            WorkItemId = "work-1",
            Suid = "suid",
            Status = SearchStatus.Completed,
            Items = [CreateSearchResultEntry()],
            CompletenessPercentage = 100,
        };
        _jobSearchService
            .GetSearchResultsAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(dto);

        var response = await _sut.SearchResultsV2Trigger(
            _httpRequestData,
            _client,
            _authFunctionContext,
            "workItem-1",
            CancellationToken.None
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenJobNotFound()
    {
        _jobSearchService
            .GetSearchResultsAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new NotFound());

        var response = await _sut.SearchResultsV2Trigger(
            _httpRequestData,
            _client,
            _authFunctionContext,
            "workItem-2",
            CancellationToken.None
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenUnauthorized()
    {
        _jobSearchService
            .GetSearchResultsAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new Unauthorized());

        var response = await _sut.SearchResultsV2Trigger(
            _httpRequestData,
            _client,
            _authFunctionContext,
            "workItem-3",
            CancellationToken.None
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReturnsInternalServerError_OnError()
    {
        _jobSearchService
            .GetSearchResultsAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new Error());

        var response = await _sut.SearchResultsV2Trigger(
            _httpRequestData,
            _client,
            _authFunctionContext,
            "workItem-4",
            CancellationToken.None
        );

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNoAuthContext()
    {
        var context = Substitute.For<FunctionContext>();
        context.Items.Returns(new Dictionary<object, object>());
        context.InvocationId.Returns(Guid.NewGuid().ToString());
        var httpRequestData = MockHttpRequestData.Create(
            new Dictionary<string, StringValues> { { "workItemId", WorkItemId } }
        );

        var response = await _sut.SearchResultsV2Trigger(
            httpRequestData,
            _client,
            context,
            "workItem-5",
            CancellationToken.None
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
