using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using SUI.Find.Application.Models;
using SUI.Find.Application.Services;
using SUI.Find.FindApi.Functions.HttpFunctions;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.UnitTests.Mocks;
using SearchStatus = SUI.Find.Application.Enums.SearchStatus;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class SearchResultsFunctionTests
{
    private readonly ILogger<SearchResultsFunction> _logger = Substitute.For<
        ILogger<SearchResultsFunction>
    >();

    private readonly ISearchService _searchService = Substitute.For<ISearchService>();
    private readonly DurableTaskClient _client = Substitute.For<DurableTaskClient>("name");
    private readonly SearchResultsFunction _sut;
    private const string JobId = "test-job-id";

    private readonly HttpRequestData _httpRequestData = MockHttpRequestData.Create(
        new Dictionary<string, StringValues> { { "jobId", JobId } }
    );

    private readonly FunctionContext _authFunctionContext = CreateContextWithAuth();

    public SearchResultsFunctionTests()
    {
        _sut = new SearchResultsFunction(_logger, _searchService);
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

    private static SearchResultItem CreateSearchResultItem()
    {
        return new SearchResultItem(
            "TestProviderSystem",
            "TestProviderName",
            "Health",
            "http://example.com/record/1"
        );
    }

    [Fact]
    public async Task ReturnsSuccess_WhenResultsFound()
    {
        var dto = new SearchResultsDto()
        {
            JobId = "job-1",
            Suid = "suid",
            Status = SearchStatus.Completed,
            Items = [CreateSearchResultItem()],
        };
        _searchService
            .GetSearchResultsAsync("job-1", "test-client-id", _client, Arg.Any<CancellationToken>())
            .Returns(new SearchResult.Success(dto));

        var response = await _sut.SearchResultsTrigger(
            _httpRequestData,
            _client,
            _authFunctionContext,
            "job-1",
            CancellationToken.None
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenJobNotFound()
    {
        _searchService
            .GetSearchResultsAsync("job-2", "test-client-id", _client, Arg.Any<CancellationToken>())
            .Returns(new SearchResult.NotFound());

        var response = await _sut.SearchResultsTrigger(
            _httpRequestData,
            _client,
            _authFunctionContext,
            "job-2",
            CancellationToken.None
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenUnauthorized()
    {
        _searchService
            .GetSearchResultsAsync("job-3", "test-client-id", _client, Arg.Any<CancellationToken>())
            .Returns(new SearchResult.Unauthorized());

        var response = await _sut.SearchResultsTrigger(
            _httpRequestData,
            _client,
            _authFunctionContext,
            "job-3",
            CancellationToken.None
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReturnsInternalServerError_OnError()
    {
        _searchService
            .GetSearchResultsAsync("job-4", "test-client-id", _client, Arg.Any<CancellationToken>())
            .Returns(new SearchResult.Failed());

        var response = await _sut.SearchResultsTrigger(
            _httpRequestData,
            _client,
            _authFunctionContext,
            "job-4",
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
            new Dictionary<string, StringValues> { { "jobId", JobId } }
        );

        var response = await _sut.SearchResultsTrigger(
            httpRequestData,
            _client,
            context,
            "job-5",
            CancellationToken.None
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
