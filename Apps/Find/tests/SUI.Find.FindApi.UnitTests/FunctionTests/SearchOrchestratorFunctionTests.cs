// Disable the Durable Functions analyzer rules for this file - this file contains only tests, and the actual Azure Functions do exist in the real code
#pragma warning disable DURABLE2003
#pragma warning disable DURABLE2004

using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Models;
using SUI.Find.Application.Models.Pep;
using SUI.Find.FindApi.Functions.ActivityFunctions;
using SUI.Find.FindApi.Functions.OrchestratorFunctions;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class SearchOrchestratorFunctionsTests
{
    private readonly TaskOrchestrationContext _mockContext;
    private readonly ILogger<SearchOrchestrator> _mockLogger = Substitute.For<
        ILogger<SearchOrchestrator>
    >();
    private readonly SearchOrchestrator _orchestrator;

    public SearchOrchestratorFunctionsTests()
    {
        _mockContext = Substitute.For<TaskOrchestrationContext>();
        _orchestrator = new SearchOrchestrator(_mockLogger);
    }

    [Fact]
    public async Task RunOrchestrator_HasValidInput_Success()
    {
        // Arrange
        var input = ArrangeSuccessfulSearchOrchestration();

        // Act
        var result = await _orchestrator.RunOrchestrator(_mockContext);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(
            result,
            r => r is { CustodianId: "org1", RecordUrl: "http://url1", SystemId: "System A" }
        );
        Assert.Contains(
            result,
            r => r is { CustodianId: "org2", RecordUrl: "http://url2", SystemId: "System B" }
        );

        await _mockContext
            .Received(1)
            .CallActivityAsync<IReadOnlyList<ProviderDefinition>>(
                "GetProvidersFunction",
                input.Suid,
                Arg.Any<TaskOptions>()
            );

        await _mockContext
            .Received(2)
            .CallActivityAsync<IReadOnlyList<CustodianSearchResultItem>>(
                "QueryProvidersFunction",
                Arg.Any<QueryProviderInput>(),
                Arg.Any<TaskOptions>()
            );

        await _mockContext
            .Received(2)
            .CallActivityAsync<IReadOnlyList<PepResultItem<CustodianSearchResultItem>>>(
                "FilterResultsByPolicyFunction",
                Arg.Any<FilterResultsInput>(),
                Arg.Any<TaskOptions>()
            );

        await _mockContext
            .Received(2)
            .CallActivityAsync(
                "PersistSearchResultsFunction",
                Arg.Any<PersistSearchResultsInput>(),
                Arg.Any<TaskOptions>()
            );

        // Assert the activity calls again, more specifically
        await _mockContext
            .Received(1)
            .CallActivityAsync<IReadOnlyList<CustodianSearchResultItem>>(
                "QueryProvidersFunction",
                Arg.Is<QueryProviderInput>(x =>
                    x.JobId == "instance-123"
                    && x.RequestingOrg == "test-client-1"
                    && x.Suid == "1234567890123456"
                    && x.Provider.OrgId == "org1"
                ),
                Arg.Any<TaskOptions>()
            );

        await _mockContext
            .Received(1)
            .CallActivityAsync<IReadOnlyList<PepResultItem<CustodianSearchResultItem>>>(
                "FilterResultsByPolicyFunction",
                Arg.Is<FilterResultsInput>(x =>
                    x.DestOrgId == "test-client-1" && x.SourceOrgId == "org1"
                ),
                Arg.Any<TaskOptions>()
            );

        await _mockContext
            .Received(1)
            .CallActivityAsync(
                "PersistSearchResultsFunction",
                Arg.Is<PersistSearchResultsInput>(x =>
                    x.WorkItemId == "instance-123"
                    && x.JobId == "instance-123"
                    && x.RequestingOrdId == "test-client-1"
                    && x.SourceOrgId == "org1"
                ),
                Arg.Any<TaskOptions>()
            );
    }

    [Fact]
    public async Task ShouldCallPepAuditActivity_WhenResultsArePresent()
    {
        // Arrange
        var input = ArrangeSuccessfulSearchOrchestration();

        // Act
        await _orchestrator.RunOrchestrator(_mockContext);

        // Assert
        await _mockContext
            .Received(1)
            .CallActivityAsync(
                nameof(AuditPepFindActivity),
                Arg.Is<AuditPepFindInput>(i =>
                    i.PolicyContext == input.PolicyContext
                    && i.Metadata == input.Metadata
                    && i.SearchResultsWithDecisions.Count == 2
                ),
                Arg.Any<TaskOptions>()
            );
    }

    [Fact]
    public async Task RunOrchestrator_NoProvidersFound_ReturnsEmptyListAndLogsWarning()
    {
        // Arrange
        var input = new SearchOrchestratorInput(
            Suid: "1234567890123456",
            Metadata: new SearchJobMetadata("person-123", DateTime.UtcNow, "invocation-123"),
            PolicyContext: new PolicyContext("test-client-1", "SAFEGUARDING", "LOCAL_AUTHORITY")
        );

        _mockContext.GetInput<SearchOrchestratorInput>().Returns(input);
        _mockContext.InstanceId.Returns("instance-123");

        _mockContext
            .CallActivityAsync<List<ProviderDefinition>>(
                "GetProvidersFunction",
                input.Suid,
                Arg.Any<TaskOptions>()
            )
            .Returns(new List<ProviderDefinition>());

        // Act
        var result = await _orchestrator.RunOrchestrator(_mockContext);

        // Assert
        Assert.Empty(result);
        await _mockContext
            .DidNotReceive()
            .CallActivityAsync<IReadOnlyList<CustodianSearchResultItem>>(
                "QueryProvidersFunction",
                Arg.Any<QueryProviderInput>(),
                Arg.Any<TaskOptions>()
            );
    }

    [Fact]
    public async Task RunOrchestrator_InvalidInput_ThrowsException()
    {
        // Arrange
        var input = new SearchOrchestratorInput(
            Suid: "",
            Metadata: new SearchJobMetadata("person-123", DateTime.UtcNow, "invocation-123"),
            PolicyContext: new PolicyContext("test-client-1", "SAFEGUARDING", "LOCAL_AUTHORITY")
        );

        _mockContext.GetInput<SearchOrchestratorInput>().Returns(input);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _orchestrator.RunOrchestrator(_mockContext)
        );
    }

    private SearchOrchestratorInput ArrangeSuccessfulSearchOrchestration()
    {
        const string destOrgId = "test-client-1";

        var input = new SearchOrchestratorInput(
            Suid: "1234567890123456",
            Metadata: new SearchJobMetadata("person-123", DateTime.UtcNow, "invocation-123"),
            PolicyContext: new PolicyContext(destOrgId, "SAFEGUARDING", "LOCAL_AUTHORITY")
        );

        _mockContext.GetInput<SearchOrchestratorInput>().Returns(input);
        _mockContext.InstanceId.Returns("instance-123");

        const string sourceOrgId1 = "org1";
        const string sourceOrgId2 = "org2";

        IReadOnlyList<ProviderDefinition> providers = new List<ProviderDefinition>
        {
            new()
            {
                OrgId = sourceOrgId1,
                OrgName = "Provider 1",
                OrgType = "Type A",
                ProviderSystem = "System A",
                ProviderName = "Provider Name 1",
                RecordType = "RecordA",
                DsaPolicy = new DsaPolicyDefinition(),
            },
            new()
            {
                OrgId = sourceOrgId2,
                OrgName = "Provider 2",
                OrgType = "Type B",
                ProviderSystem = "System B",
                ProviderName = "Provider Name 2",
                RecordType = "RecordB",
                DsaPolicy = new DsaPolicyDefinition(),
            },
        };

        _mockContext
            .CallActivityAsync<IReadOnlyList<ProviderDefinition>>(
                "GetProvidersFunction",
                input.Suid,
                Arg.Any<TaskOptions>()
            )
            .Returns(providers);

        // Pass on the call to the sub-orchestrator like for real, so we get an end-to-end test of the main `SearchOrchestrator`
        _mockContext
            .CallSubOrchestratorAsync<IReadOnlyList<PepResultItem<CustodianSearchResultItem>>>(
                "SearchProviderSubOrchestrator",
                Arg.Any<SearchProviderSubOrchestratorInput>()
            )
            .Returns(callInfo =>
            {
                var subOrchestratorInput = callInfo.Arg<SearchProviderSubOrchestratorInput>();
                _mockContext
                    .GetInput<SearchProviderSubOrchestratorInput>()
                    .Returns(subOrchestratorInput);
                return _orchestrator.SearchProviderSubOrchestrator(_mockContext);
            });

        // Unfiltered query results per provider
        var queryResultOrg1 = new List<CustodianSearchResultItem>
        {
            new(sourceOrgId1, "RecordA", "http://url1", "System A", "test org 1", "TestRecord 1"),
        };
        var queryResultOrg2 = new List<CustodianSearchResultItem>
        {
            new(sourceOrgId2, "RecordB", "http://url2", "System B", "test org 2", "TestRecord 2"),
        };

        _mockContext
            .CallActivityAsync<IReadOnlyList<CustodianSearchResultItem>>(
                "QueryProvidersFunction",
                Arg.Is<QueryProviderInput>(i => i.Provider.OrgId == sourceOrgId1),
                Arg.Any<TaskOptions>()
            )
            .Returns(queryResultOrg1);

        _mockContext
            .CallActivityAsync<IReadOnlyList<CustodianSearchResultItem>>(
                "QueryProvidersFunction",
                Arg.Is<QueryProviderInput>(i => i.Provider.OrgId == sourceOrgId2),
                Arg.Any<TaskOptions>()
            )
            .Returns(queryResultOrg2);

        // Filtered results with decisions
        var filteredResultOrg1 = new List<PepResultItem<CustodianSearchResultItem>>
        {
            new(
                queryResultOrg1[0],
                sourceOrgId1,
                destOrgId,
                new PolicyDecisionResult() { IsAllowed = true, Reason = "Allowed" }
            ),
        };
        var filteredResultOrg2 = new List<PepResultItem<CustodianSearchResultItem>>
        {
            new(
                queryResultOrg2[0],
                sourceOrgId2,
                destOrgId,
                new PolicyDecisionResult() { IsAllowed = true, Reason = "Allowed" }
            ),
        };

        _mockContext
            .CallActivityAsync<IReadOnlyList<PepResultItem<CustodianSearchResultItem>>>(
                "FilterResultsByPolicyFunction",
                Arg.Is<FilterResultsInput>(i =>
                    i.SourceOrgId == sourceOrgId1 && i.Items.Count == 1
                ),
                Arg.Any<TaskOptions>()
            )
            .Returns(filteredResultOrg1);

        _mockContext
            .CallActivityAsync<IReadOnlyList<PepResultItem<CustodianSearchResultItem>>>(
                "FilterResultsByPolicyFunction",
                Arg.Is<FilterResultsInput>(i =>
                    i.SourceOrgId == sourceOrgId2 && i.Items.Count == 1
                ),
                Arg.Any<TaskOptions>()
            )
            .Returns(filteredResultOrg2);

        return input;
    }
}
