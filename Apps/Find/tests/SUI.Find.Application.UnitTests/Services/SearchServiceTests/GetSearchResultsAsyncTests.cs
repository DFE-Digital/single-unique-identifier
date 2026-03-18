using System.Text.Json;
using Microsoft.DurableTask.Client;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OneOf.Types;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Models;

namespace SUI.Find.Application.UnitTests.Services.SearchServiceTests;

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
                    new()
                    {
                        RecordType = "Health",
                        RecordUrl = "http://example.com/record/1",
                        SystemId = "TestSystem",
                        RecordId = "TestRecord",
                    },
                    new()
                    {
                        RecordType = "Education",
                        RecordUrl = "http://example.com/record/2",
                        SystemId = "TestSystem",
                        RecordId = "TestRecord",
                    },
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

    [Fact]
    public async Task ShouldReturnPersistedResults_WhenJobIsRunning()
    {
        var meta = new OrchestrationMetadata("Orchestrator", "running-job")
        {
            RuntimeStatus = OrchestrationRuntimeStatus.Running,
        };

        _client.GetInstanceAsync("running-job", true, Arg.Any<CancellationToken>()).Returns(meta);

        SearchResultEntryRepository
            .GetByWorkItemIdAsync("running-job", ClientId, Arg.Any<CancellationToken>())
            .Returns(
                new[]
                {
                    new SearchResultEntry
                    {
                        CustodianId = "Health",
                        SystemId = "SystemA",
                        CustodianName = "Org1",
                        RecordType = "Type1",
                        RecordUrl = "url1",
                        RecordId = "12345",
                        JobId = "running-job",
                        SubmittedAtUtc = DateTimeOffset.UtcNow,
                        WorkItemId = "running-job",
                        SearchingOrganisationId = ClientId,
                    },
                }
            );

        var result = await Sut.GetSearchResultsAsync(
            "running-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        var body = Assert.IsType<SearchResultsDto>(result.Value);

        Assert.Single(body.Items);
        Assert.Equal("SystemA", body.Items[0].SystemId);
        Assert.Equal("Org1", body.Items[0].CustodianName);
        Assert.Equal("Type1", body.Items[0].RecordType);
        Assert.Equal("url1", body.Items[0].RecordUrl);
        Assert.Equal("12345", body.Items[0].RecordId);
    }

    [Fact]
    public async Task ShouldNotReturnPersistedResults_FromPreviousSearchExecutions_WhenJobIsRunning()
    {
        var searchCreatedAt = DateTimeOffset.UtcNow;

        var meta = new OrchestrationMetadata("Orchestrator", "running-job")
        {
            RuntimeStatus = OrchestrationRuntimeStatus.Running,
            CreatedAt = searchCreatedAt,
        };

        _client.GetInstanceAsync("running-job", true, Arg.Any<CancellationToken>()).Returns(meta);

        SearchResultEntryRepository
            .GetByWorkItemIdAsync("running-job", ClientId, Arg.Any<CancellationToken>())
            .Returns([
                new SearchResultEntry
                {
                    SubmittedAtUtc = searchCreatedAt.AddSeconds(-1),
                    RecordUrl = "old-result",
                    CustodianId = "",
                    SystemId = "",
                    CustodianName = "",
                    RecordType = "",
                    JobId = "running-job",
                    WorkItemId = "running-job",
                    SearchingOrganisationId = ClientId,
                },
                new SearchResultEntry
                {
                    SubmittedAtUtc = searchCreatedAt,
                    RecordUrl = "new-result",
                    CustodianId = "",
                    SystemId = "",
                    CustodianName = "",
                    RecordType = "",
                    JobId = "running-job",
                    WorkItemId = "running-job",
                    SearchingOrganisationId = ClientId,
                },
            ]);

        // ACT
        var result = await Sut.GetSearchResultsAsync(
            "running-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        // ASSERT
        var body = Assert.IsType<SearchResultsDto>(result.Value);

        var item = Assert.Single(body.Items);
        Assert.Equal("new-result", item.RecordUrl);
    }

    [Fact]
    public async Task ShouldReturnOrchestratorResults_WhenJobIsCompleted()
    {
        var orchestratorItems = new[]
        {
            new SearchResultItem()
            {
                RecordType = "Type1",
                RecordUrl = "Url1",
                SystemId = "SystemA",
                RecordId = "12345",
            },
        };

        var meta = new OrchestrationMetadata("Orchestrator", "completed-job")
        {
            RuntimeStatus = OrchestrationRuntimeStatus.Completed,
            SerializedOutput = JsonSerializer.Serialize(orchestratorItems),
        };

        _client.GetInstanceAsync("completed-job", true, Arg.Any<CancellationToken>()).Returns(meta);

        SearchResultEntryRepository
            .GetByWorkItemIdAsync("completed-job", ClientId, Arg.Any<CancellationToken>())
            .Returns(
                new[]
                {
                    new SearchResultEntry
                    {
                        CustodianId = "Education",
                        SystemId = "SystemB",
                        CustodianName = "Org1",
                        RecordType = "Type2",
                        RecordUrl = "url2",
                        RecordId = "99999",
                        JobId = "completed-job",
                        SubmittedAtUtc = DateTimeOffset.UtcNow,
                        WorkItemId = "completed-job",
                        SearchingOrganisationId = ClientId,
                    },
                }
            );

        var result = await Sut.GetSearchResultsAsync(
            "completed-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        var body = Assert.IsType<SearchResultsDto>(result.Value);

        Assert.Single(body.Items);
        Assert.Equal("SystemA", body.Items[0].SystemId);
        Assert.Equal("Type1", body.Items[0].RecordType);
        Assert.Equal("Url1", body.Items[0].RecordUrl);
        Assert.Equal("12345", body.Items[0].RecordId);
    }

    [Fact]
    public async Task ShouldDefaultSystemId_WhenPersistedResultHasNullOrEmptySystemId()
    {
        var meta = new OrchestrationMetadata("Orchestrator", "running-job")
        {
            RuntimeStatus = OrchestrationRuntimeStatus.Running,
        };

        _client.GetInstanceAsync("running-job", true, Arg.Any<CancellationToken>()).Returns(meta);

        SearchResultEntryRepository
            .GetByWorkItemIdAsync("running-job", ClientId, Arg.Any<CancellationToken>())
            .Returns(
                new[]
                {
                    new SearchResultEntry
                    {
                        CustodianId = "Health",
                        SystemId = "", // simulate bad persisted data
                        CustodianName = "Org1",
                        RecordType = "Type1",
                        RecordUrl = "url1",
                        RecordId = "12345",
                        JobId = "running-job",
                        SubmittedAtUtc = DateTimeOffset.UtcNow,
                        WorkItemId = "running-job",
                        SearchingOrganisationId = ClientId,
                    },
                }
            );

        var result = await Sut.GetSearchResultsAsync(
            "running-job",
            ClientId,
            _client,
            CancellationToken.None
        );

        var body = Assert.IsType<SearchResultsDto>(result.Value);

        Assert.Single(body.Items);
        Assert.Equal("DefaultSystem", body.Items[0].SystemId);
    }
}
