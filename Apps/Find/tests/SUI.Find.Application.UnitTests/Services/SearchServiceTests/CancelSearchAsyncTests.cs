using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OneOf.Types;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Services;

namespace SUI.Find.Application.UnitTests.Services.SearchServiceTests;

public class CancelSearchAsyncTests : BaseSearchServiceTests
{
    private readonly DurableTaskClient _client = Substitute.For<DurableTaskClient>("name");
    private const string ClientId = "test-client-id";

    public CancelSearchAsyncTests()
    {
        var metaData = new SearchJobMetadata("test-person-id", DateTime.UtcNow, "invocation-id");
        var policyData = new PolicyContext("test-client-id", [], "SAFEGUARDING", "LOCAL_AUTHORITY");
        Sut.ReadOrchestratorInput<SearchOrchestratorInput>(Arg.Any<OrchestrationMetadata>())
            .Returns(new SearchOrchestratorInput("test-suid", metaData, policyData));
    }

    [Fact]
    public async Task ShouldReturnNotFound_WhenJobDoesNotExist()
    {
        _client
            .GetInstanceAsync("not-found-job", Arg.Any<CancellationToken>())
            .Returns((OrchestrationMetadata?)null);

        var result = await Sut.CancelSearchAsync(
            "not-found-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        Assert.IsType<NotFound>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnAlreadyCompleted_WhenJobIsCompleted()
    {
        var meta = new OrchestrationMetadata("Orchestrator", "completed-job")
        {
            RuntimeStatus = OrchestrationRuntimeStatus.Completed,
        };
        _client.GetInstanceAsync("completed-job", Arg.Any<CancellationToken>()).Returns(meta);

        var result = await Sut.CancelSearchAsync(
            "completed-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        var body = Assert.IsType<SearchJobDto>(result.Value);
        Assert.Equal("completed-job", body.JobId);
    }

    [Fact]
    public async Task ShouldReturnCannotCancel_WhenJobIsInNonCancellableState()
    {
        var meta = new OrchestrationMetadata("Orchestrator", "non-cancellable-job")
        {
            RuntimeStatus = OrchestrationRuntimeStatus.Suspended,
        };
        _client.GetInstanceAsync("non-cancellable-job", Arg.Any<CancellationToken>()).Returns(meta);

        var result = await Sut.CancelSearchAsync(
            "non-cancellable-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        var body = Assert.IsType<SearchJobDto>(result.Value);
        Assert.NotNull(body);
        Assert.Equal("non-cancellable-job", body.JobId);
    }

    [Fact]
    public async Task ShouldReturnCanceled_WhenTerminationSucceeds()
    {
        var meta = new OrchestrationMetadata("Orchestrator", "cancel-job")
        {
            RuntimeStatus = OrchestrationRuntimeStatus.Running,
        };
        _client.GetInstanceAsync("cancel-job", Arg.Any<CancellationToken>()).Returns(meta);

        var result = await Sut.CancelSearchAsync(
            "cancel-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        var body = Assert.IsType<SearchJobDto>(result.Value);
        Assert.NotNull(body);
        Assert.Equal("test-suid", body.PersonId);
    }

    [Fact]
    public async Task ShouldReturnFailed_WhenExceptionIsThrown()
    {
        _client
            .GetInstanceAsync("fail-job", Arg.Any<CancellationToken>())
            .Throws(new Exception("fail"));

        var result = await Sut.CancelSearchAsync(
            "fail-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        Assert.IsType<Error>(result.Value);
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
        var metaData = new SearchJobMetadata("test-person-id", DateTime.UtcNow, "invocation-id");
        var policyData = new PolicyContext(
            "different-client-id",
            [],
            "SAFEGUARDING",
            "LOCAL_AUTHORITY"
        );
        Sut.ReadOrchestratorInput<SearchOrchestratorInput>(meta)
            .Returns(new SearchOrchestratorInput("test-suid", metaData, policyData));

        var result = await Sut.CancelSearchAsync(
            "unauth-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        Assert.IsType<Unauthorized>(result.Value);
    }
}
