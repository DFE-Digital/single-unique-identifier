using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Models;
using SUI.Find.Application.Services;

namespace SUI.Find.ApplicationTests.Services.SearchServiceTests;

public class GetSearchStatusAsyncTests
{
    private readonly DurableTaskClient _client = Substitute.For<DurableTaskClient>("name");
    private readonly SearchService _searchService;
    private const string ClientId = "test-client-id";

    public GetSearchStatusAsyncTests()
    {
        var metaData = new SearchJobMetadata("test-person-id", DateTime.UtcNow);
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

        var result = await _searchService.GetSearchStatusAsync(
            "not-found-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        var bob = result.GetType();

        Assert.Equal(typeof(SearchJobResult.NotFound), bob);
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
        var metaData = new SearchJobMetadata("test-person-id", DateTime.UtcNow);
        var policyData = new PolicyContext("different-client-id", []);
        _searchService
            .ReadOrchestratorInput<SearchOrchestratorInput>(meta)
            .Returns(new SearchOrchestratorInput("test-suid", metaData, policyData));

        var result = await _searchService.GetSearchStatusAsync(
            "unauth-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        Assert.Equal(typeof(SearchJobResult.Unauthorized), result.GetType());
    }

    [Fact]
    public async Task ShouldReturnSuccess_WhenJobExistsAndClientIdMatches()
    {
        var meta = new OrchestrationMetadata("Orchestrator", "auth-job")
        {
            RuntimeStatus = OrchestrationRuntimeStatus.Running,
        };
        _client.GetInstanceAsync("auth-job", true, Arg.Any<CancellationToken>()).Returns(meta);
        var metaData = new SearchJobMetadata("test-person-id", DateTime.UtcNow);
        var policyData = new PolicyContext(ClientId, []);
        _searchService
            .ReadOrchestratorInput<SearchOrchestratorInput>(meta)
            .Returns(new SearchOrchestratorInput("test-suid", metaData, policyData));

        var result = await _searchService.GetSearchStatusAsync(
            "auth-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        var successResult = Assert.IsType<SearchJobResult.Success>(result);
        Assert.Equal("test-suid", successResult.Job.Suid);
        Assert.Equal("auth-job", successResult.Job.JobId);
        Assert.Equal(SearchStatus.Running, successResult.Job.Status);
    }

    [Fact]
    public async Task ShouldReturnFailed_WhenExceptionIsThrown()
    {
        _client
            .GetInstanceAsync("error-job", true, Arg.Any<CancellationToken>())
            .Throws(new Exception("Test exception"));

        var result = await _searchService.GetSearchStatusAsync(
            "error-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        Assert.Equal(typeof(SearchJobResult.Failed), result.GetType());
    }
}
