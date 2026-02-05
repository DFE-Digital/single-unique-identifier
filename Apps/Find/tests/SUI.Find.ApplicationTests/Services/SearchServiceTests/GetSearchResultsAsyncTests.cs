using System.Text.Json;
using Microsoft.DurableTask.Client;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OneOf.Types;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Models;

namespace SUI.Find.ApplicationTests.Services.SearchServiceTests;

public class GetSearchResultsAsyncTests : BaseSearchServiceTests
{
    private readonly DurableTaskClient _client = Substitute.For<DurableTaskClient>("name");
    private const string ClientId = "test-client-id";

    [Fact]
    public async Task ShouldReturnNotFound_WhenJobDoesNotExist()
    {
        _client
            .GetInstanceAsync("not-found-job", Arg.Any<CancellationToken>())
            .Returns((OrchestrationMetadata?)null);

        var result = await Sut.GetSearchResultsAsync(
            "not-found-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        Assert.IsType<NotFound>(result.Value);
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
        var policyData = new PolicyContext(
            "different-client-id",
            [],
            "SAFEGUARDING",
            "LOCAL_AUTHORITY"
        );
        Sut.ReadOrchestratorInput<SearchOrchestratorInput>(meta)
            .Returns(new SearchOrchestratorInput("test-suid", metaData, policyData));

        var result = await Sut.GetSearchResultsAsync(
            "unauth-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        Assert.IsType<Unauthorized>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnFailed_WhenExceptionIsThrown()
    {
        _client
            .GetInstanceAsync("fail-job", true, Arg.Any<CancellationToken>())
            .Throws(new Exception("fail"));

        var result = await Sut.GetSearchResultsAsync(
            "fail-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        Assert.IsType<Error>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnSuccess_EvenWhenJobIsNotCompleted()
    {
        var meta = new OrchestrationMetadata("Orchestrator", "running-job")
        {
            RuntimeStatus = OrchestrationRuntimeStatus.Running,
        };
        _client.GetInstanceAsync("running-job", true, Arg.Any<CancellationToken>()).Returns(meta);

        var result = await Sut.GetSearchResultsAsync(
            "running-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        var body = Assert.IsType<SearchResultsDto>(result.Value);
        Assert.Equal("test-suid", body.Suid);
        Assert.Equal(SearchStatus.Running, body.Status);
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

        var result = await Sut.GetSearchResultsAsync(
            "empty-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        var body = Assert.IsType<SearchResultsDto>(result.Value);
        Assert.Equal("test-suid", body.Suid);
        Assert.Equal(SearchStatus.Completed, body.Status);
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
                    new("Health", "http://example.com/record/1", "TestSystem", "TestRecord"),
                    new("Education", "http://example.com/record/2", "TestSystem", "TestRecord"),
                }
            ),
        };
        _client.GetInstanceAsync("success-job", true, Arg.Any<CancellationToken>()).Returns(meta);

        var result = await Sut.GetSearchResultsAsync(
            "success-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        var body = Assert.IsType<SearchResultsDto>(result.Value);
        Assert.Equal("test-suid", body.Suid);
        Assert.Equal(SearchStatus.Completed, body.Status);
        Assert.Equal(2, body.Items.Length);
    }
}
