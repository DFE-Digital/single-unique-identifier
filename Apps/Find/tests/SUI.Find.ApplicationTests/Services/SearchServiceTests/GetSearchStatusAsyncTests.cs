using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OneOf.Types;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Models;
using SUI.Find.Application.Services;

namespace SUI.Find.ApplicationTests.Services.SearchServiceTests;

public class GetSearchStatusAsyncTests : BaseSearchServiceTests
{
    private readonly DurableTaskClient _client = Substitute.For<DurableTaskClient>("name");
    private const string ClientId = "test-client-id";

    [Fact]
    public async Task ShouldReturnNotFound_WhenJobDoesNotExist()
    {
        _client
            .GetInstanceAsync("not-found-job", Arg.Any<CancellationToken>())
            .Returns((OrchestrationMetadata?)null);

        var result = await Sut.GetSearchStatusAsync(
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

        var result = await Sut.GetSearchStatusAsync(
            "unauth-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        Assert.IsType<Unauthorized>(result.Value);
    }

    [Fact]
    public async Task ShouldReturnSuccess_WhenJobExistsAndClientIdMatches()
    {
        var meta = new OrchestrationMetadata("Orchestrator", "auth-job")
        {
            RuntimeStatus = OrchestrationRuntimeStatus.Running,
        };
        _client.GetInstanceAsync("auth-job", true, Arg.Any<CancellationToken>()).Returns(meta);
        var metaData = new SearchJobMetadata("test-person-id", DateTime.UtcNow, "invocation-id");
        var policyData = new PolicyContext(ClientId, [], "SAFEGUARDING", "LOCAL_AUTHORITY");
        Sut.ReadOrchestratorInput<SearchOrchestratorInput>(meta)
            .Returns(new SearchOrchestratorInput("test-suid", metaData, policyData));

        var result = await Sut.GetSearchStatusAsync(
            "auth-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        var successResult = Assert.IsType<SearchJobDto>(result.Value);
        Assert.Equal("test-suid", successResult.PersonId);
        Assert.Equal("auth-job", successResult.JobId);
        Assert.Equal(SearchStatus.Running, successResult.Status);
    }

    [Fact]
    public async Task ShouldReturnFailed_WhenExceptionIsThrown()
    {
        _client
            .GetInstanceAsync("error-job", true, Arg.Any<CancellationToken>())
            .Throws(new Exception("Test exception"));

        var result = await Sut.GetSearchStatusAsync(
            "error-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        Assert.IsType<Error>(result.Value);
    }
}
