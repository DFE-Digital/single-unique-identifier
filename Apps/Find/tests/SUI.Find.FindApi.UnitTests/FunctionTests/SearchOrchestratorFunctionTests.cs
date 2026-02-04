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
        Assert.Contains(result, r => r.CustodianId == "System A");
        Assert.Contains(result, r => r.CustodianId == "System B");

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
            PolicyContext: new PolicyContext(
                "test-client-1",
                ["scope1", "scope2"],
                "SAFEGUARDING",
                "LOCAL_AUTHORITY"
            )
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
            .CallActivityAsync<IReadOnlyList<SearchResultItem>>(
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
            PolicyContext: new PolicyContext(
                "test-client-1",
                ["scope1", "scope2"],
                "SAFEGUARDING",
                "LOCAL_AUTHORITY"
            )
        );

        _mockContext.GetInput<SearchOrchestratorInput>().Returns(input);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _orchestrator.RunOrchestrator(_mockContext)
        );
    }

    private SearchOrchestratorInput ArrangeSuccessfulSearchOrchestration()
    {
        var input = new SearchOrchestratorInput(
            Suid: "1234567890123456",
            Metadata: new SearchJobMetadata("person-123", DateTime.UtcNow, "invocation-123"),
            PolicyContext: new PolicyContext(
                "test-client-1",
                ["scope1", "scope2"],
                "SAFEGUARDING",
                "LOCAL_AUTHORITY"
            )
        );

        _mockContext.GetInput<SearchOrchestratorInput>().Returns(input);
        _mockContext.InstanceId.Returns("instance-123");

        IReadOnlyList<ProviderDefinition> providers = new List<ProviderDefinition>
        {
            new()
            {
                OrgId = "org1",
                OrgName = "Provider 1",
                OrgType = "Type A",
                ProviderSystem = "System A",
                ProviderName = "Provider Name 1",
                RecordType = "RecordA",
                DsaPolicy = new DsaPolicyDefinition(),
            },
            new()
            {
                OrgId = "org2",
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

        // Unfiltered query results per provider
        var queryResultOrg1 = new List<CustodianSearchResultItem>
        {
            new(null, "Provider Name 1", "RecordA", "http://url1", "System A"),
        };
        var queryResultOrg2 = new List<CustodianSearchResultItem>
        {
            new(null, "Provider Name 2", "RecordB", "http://url2", "System B"),
        };

        _mockContext
            .CallActivityAsync<IReadOnlyList<CustodianSearchResultItem>>(
                "QueryProvidersFunction",
                Arg.Is<QueryProviderInput>(i => i.Provider.OrgId == "org1"),
                Arg.Any<TaskOptions>()
            )
            .Returns(queryResultOrg1);

        _mockContext
            .CallActivityAsync<IReadOnlyList<CustodianSearchResultItem>>(
                "QueryProvidersFunction",
                Arg.Is<QueryProviderInput>(i => i.Provider.OrgId == "org2"),
                Arg.Any<TaskOptions>()
            )
            .Returns(queryResultOrg2);

        // Filtered results with decisions
        var filteredResultOrg1 = new List<SearchResultWithDecision>
        {
            new(
                queryResultOrg1[0],
                "org1",
                new PolicyDecisionResult() { IsAllowed = true, Reason = "Allowed" }
            ),
        };
        var filteredResultOrg2 = new List<SearchResultWithDecision>
        {
            new(
                queryResultOrg2[0],
                "org2",
                new PolicyDecisionResult() { IsAllowed = true, Reason = "Allowed" }
            ),
        };

        _mockContext
            .CallActivityAsync<IReadOnlyList<SearchResultWithDecision>>(
                "FilterResultsByPolicyFunction",
                Arg.Is<FilterResultsInput>(i => i.SourceOrgId == "org1" && i.Items.Count == 1),
                Arg.Any<TaskOptions>()
            )
            .Returns(filteredResultOrg1);

        _mockContext
            .CallActivityAsync<IReadOnlyList<SearchResultWithDecision>>(
                "FilterResultsByPolicyFunction",
                Arg.Is<FilterResultsInput>(i => i.SourceOrgId == "org2" && i.Items.Count == 1),
                Arg.Any<TaskOptions>()
            )
            .Returns(filteredResultOrg2);

        return input;
    }
}
