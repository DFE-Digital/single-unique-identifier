using Microsoft.DurableTask.Client;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Models;
using SUI.Find.Application.Services;

namespace SUI.Find.ApplicationTests.Services.SearchCancelTests;

public class CancelSearchAsyncTests
{
    private readonly DurableTaskClient _client = Substitute.For<DurableTaskClient>("name");
    private readonly SearchService _searchService = Substitute.ForPartsOf<SearchService>(null);
    private const string ClientId = "test-client-id";

    public CancelSearchAsyncTests()
    {
        var metaData = new SearchJobMetadata("test-person-id", DateTime.UtcNow);
        var policyData = new PolicyContext(ClientId, []);
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

        var result = await _searchService.CancelSearchAsync(
            "not-found-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        Assert.Equal(CancelSearchResult.NotFound, result.Result);
    }

    [Fact]
    public async Task ShouldReturnAlreadyCompleted_WhenJobIsCompleted()
    {
        var meta = new OrchestrationMetadata("Orchestrator", "completed-job")
        {
            RuntimeStatus = OrchestrationRuntimeStatus.Completed,
        };
        _client.GetInstanceAsync("completed-job", Arg.Any<CancellationToken>()).Returns(meta);

        var result = await _searchService.CancelSearchAsync(
            "completed-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        Assert.Equal(CancelSearchResult.CannotCancel, result.Result);
    }

    [Fact]
    public async Task ShouldReturnCannotCancel_WhenJobIsInNonCancellableState()
    {
        var meta = new OrchestrationMetadata("Orchestrator", "non-cancellable-job")
        {
            RuntimeStatus = OrchestrationRuntimeStatus.Suspended,
        };
        _client.GetInstanceAsync("non-cancellable-job", Arg.Any<CancellationToken>()).Returns(meta);

        var result = await _searchService.CancelSearchAsync(
            "non-cancellable-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        Assert.Equal(CancelSearchResult.CannotCancel, result.Result);
    }

    [Fact]
    public async Task ShouldReturnCanceled_WhenTerminationSucceeds()
    {
        var meta = new OrchestrationMetadata("Orchestrator", "cancel-job")
        {
            RuntimeStatus = OrchestrationRuntimeStatus.Running,
        };
        _client.GetInstanceAsync("cancel-job", Arg.Any<CancellationToken>()).Returns(meta);

        var result = await _searchService.CancelSearchAsync(
            "cancel-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        Assert.Equal(CancelSearchResult.Canceled, result.Result);
    }

    [Fact]
    public async Task ShouldReturnFailed_WhenExceptionIsThrown()
    {
        _client
            .GetInstanceAsync("fail-job", Arg.Any<CancellationToken>())
            .Throws(new Exception("fail"));

        var result = await _searchService.CancelSearchAsync(
            "fail-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        Assert.Equal(CancelSearchResult.Failed, result.Result);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ShouldReturnUnauthorized_WhenClientIdDoesNotMatch()
    {
        var meta = new OrchestrationMetadata("Orchestrator", "unauth-job")
        {
            RuntimeStatus = OrchestrationRuntimeStatus.Running,
        };
        _client.GetInstanceAsync("unauth-job", Arg.Any<CancellationToken>()).Returns(meta);

        // Mock the ReadOrchestratorInput to return a different clientId
        var metaData = new SearchJobMetadata("test-person-id", DateTime.UtcNow);
        var policyData = new PolicyContext("different-client-id", []);
        _searchService
            .ReadOrchestratorInput<SearchOrchestratorInput>(meta)
            .Returns(new SearchOrchestratorInput("test-suid", metaData, policyData));

        var result = await _searchService.CancelSearchAsync(
            "unauth-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        Assert.Equal(CancelSearchResult.Unauthorized, result.Result);
        Assert.Equal("Unauthorized", result.ErrorMessage);
    }
}
