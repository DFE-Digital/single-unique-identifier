using System.Text.Json;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Models;
using SUI.Find.Application.Services;

namespace SUI.Find.ApplicationTests.Services.SearchServiceTests;

public class GetSearchResultsAsyncTests
{
    private readonly DurableTaskClient _client = Substitute.For<DurableTaskClient>("name");
    private readonly SearchService _searchService;
    private const string ClientId = "test-client-id";

    public GetSearchResultsAsyncTests()
    {
        var metaData = new SearchJobMetadata("test-person-id", DateTime.UtcNow, "invocation-id");
        var policyData = new PolicyContext(ClientId, []);
        _searchService = Substitute.ForPartsOf<SearchService>(
            Substitute.For<ILogger<SearchService>>()
        );
        _searchService
            .ReadOrchestratorInput<SearchOrchestratorInput>(Arg.Any<OrchestrationMetadata>())
            .Returns(new SearchOrchestratorInput("test-suid", metaData, policyData));
    }

    [Fact]
    public async Task ShouldReturnNotFound_WhenJobDoesNotExist()
    {
        _client
            .GetInstanceAsync("not-found-job", Arg.Any<CancellationToken>())
            .Returns((OrchestrationMetadata?)null);

        var result = await _searchService.GetSearchResultsAsync(
            "not-found-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        Assert.IsType<SearchResult.NotFound>(result);
    }

    [Fact]
    public async Task ShouldReturnUnauthorized_WhenClientIdDoesNotMatch()
    {
        var meta = new OrchestrationMetadata("Orchestrator", "unauth-job")
        {
            RuntimeStatus = OrchestrationRuntimeStatus.Running,
        };
        _client.GetInstanceAsync("unauth-job", true, Arg.Any<CancellationToken>()).Returns(meta);

        // Mock the ReadOrchestratorInput to return a different clientId
        var metaData = new SearchJobMetadata("test-person-id", DateTime.UtcNow, "invocation-id");
        var policyData = new PolicyContext("different-client-id", []);
        _searchService
            .ReadOrchestratorInput<SearchOrchestratorInput>(meta)
            .Returns(new SearchOrchestratorInput("test-suid", metaData, policyData));

        var result = await _searchService.GetSearchResultsAsync(
            "unauth-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        Assert.IsType<SearchResult.Unauthorized>(result);
    }

    [Fact]
    public async Task ShouldReturnFailed_WhenExceptionIsThrown()
    {
        _client
            .GetInstanceAsync("fail-job", true, Arg.Any<CancellationToken>())
            .Throws(new Exception("fail"));

        var result = await _searchService.GetSearchResultsAsync(
            "fail-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        Assert.IsType<SearchResult.Failed>(result);
    }

    [Fact]
    public async Task ShouldReturnSuccess_EvenWhenJobIsNotCompleted()
    {
        var meta = new OrchestrationMetadata("Orchestrator", "running-job")
        {
            RuntimeStatus = OrchestrationRuntimeStatus.Running,
        };
        _client.GetInstanceAsync("running-job", true, Arg.Any<CancellationToken>()).Returns(meta);

        var result = await _searchService.GetSearchResultsAsync(
            "running-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        Assert.IsType<SearchResult.Success>(result);
        var successResult = result as SearchResult.Success;
        var resultDto = successResult?.Result;
        Assert.Equal("test-suid", resultDto?.Suid);
        Assert.Equal(SearchStatus.Running, resultDto?.Status);
    }

    [Fact]
    public async Task ShouldReturnSuccess_WithNoResults_WhenJobIsCompletedButNoResults()
    {
        var meta = new OrchestrationMetadata("Orchestrator", "empty-job")
        {
            RuntimeStatus = OrchestrationRuntimeStatus.Completed,
            SerializedOutput = null,
        };
        _client.GetInstanceAsync("empty-job", true, Arg.Any<CancellationToken>()).Returns(meta);

        var result = await _searchService.GetSearchResultsAsync(
            "empty-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        Assert.IsType<SearchResult.Success>(result);
        var successResult = result as SearchResult.Success;
        var resultDto = successResult?.Result;
        Assert.Equal("test-suid", resultDto?.Suid);
        Assert.Equal(SearchStatus.Completed, resultDto?.Status);
    }

    [Fact]
    public async Task ShouldReturnSuccess_WhenJobExistsAndClientIdMatches()
    {
        var meta = new OrchestrationMetadata("Orchestrator", "success-job")
        {
            RuntimeStatus = OrchestrationRuntimeStatus.Completed,
            SerializedOutput = JsonSerializer.Serialize(
                new SearchResultItem[]
                {
                    new(
                        "TestProviderSystem",
                        "TestProviderName",
                        "Health",
                        "http://example.com/record/1"
                    ),
                    new(
                        "TestProviderSystem",
                        "TestProviderName",
                        "Education",
                        "http://example.com/record/2"
                    ),
                }
            ),
        };
        _client.GetInstanceAsync("success-job", true, Arg.Any<CancellationToken>()).Returns(meta);

        var result = await _searchService.GetSearchResultsAsync(
            "success-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        Assert.IsType<SearchResult.Success>(result);
        var successResult = result as SearchResult.Success;
        var resultDto = successResult?.Result;
        Assert.Equal("test-suid", resultDto?.Suid);
        Assert.Equal(SearchStatus.Completed, resultDto?.Status);
        Assert.Equal(2, resultDto?.Items.Length);
    }
}
