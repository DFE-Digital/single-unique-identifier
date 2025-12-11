using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Models;
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
        var input = new SearchOrchestratorInput(
            Suid: "1234567890123456",
            Metadata: new SearchJobMetadata(
                PersonId: "person-123",
                DateTime.UtcNow,
                "invocation-123"
            ),
            PolicyContext: new PolicyContext(ClientId: "test-client-1", ["scope1", "scope2"])
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
            },
            new()
            {
                OrgId = "org2",
                OrgName = "Provider 2",
                OrgType = "Type B",
                ProviderSystem = "System B",
                ProviderName = "Provider Name 2",
            },
        };

        _mockContext
            .CallActivityAsync<IReadOnlyList<ProviderDefinition>>(
                "GetProvidersFunction",
                input.Suid,
                Arg.Any<TaskOptions>()
            )
            .Returns(providers);

        var result1 = new List<SearchResultItem>
        {
            new("System A", "Provider Name 1", "Record", "http://url1"),
        };
        var result2 = new List<SearchResultItem>
        {
            new("System B", "Provider Name 2", "Record", "http://url2"),
        };

        _mockContext
            .CallActivityAsync<IReadOnlyList<SearchResultItem>>(
                "QueryProvidersFunction",
                Arg.Is<QueryProviderInput>(i => i.Provider.OrgId == "org1"),
                Arg.Any<TaskOptions>()
            )
            .Returns(result1);

        _mockContext
            .CallActivityAsync<IReadOnlyList<SearchResultItem>>(
                "QueryProvidersFunction",
                Arg.Is<QueryProviderInput>(i => i.Provider.OrgId == "org2"),
                Arg.Any<TaskOptions>()
            )
            .Returns(result2);

        // Act
        var result = await _orchestrator.RunOrchestrator(_mockContext);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.ProviderSystem == "System A");
        Assert.Contains(result, r => r.ProviderSystem == "System B");

        await _mockContext
            .Received(1)
            .CallActivityAsync<IReadOnlyList<ProviderDefinition>>(
                "GetProvidersFunction",
                input.Suid,
                Arg.Any<TaskOptions>()
            );
        await _mockContext
            .Received(2)
            .CallActivityAsync<IReadOnlyList<SearchResultItem>>(
                "QueryProvidersFunction",
                Arg.Any<QueryProviderInput>(),
                Arg.Any<TaskOptions>()
            );
    }

    [Fact]
    public async Task RunOrchestrator_NoProvidersFound_ReturnsEmptyListAndLogsWarning()
    {
        // Arrange
        var input = new SearchOrchestratorInput(
            Suid: "1234567890123456",
            Metadata: new SearchJobMetadata(
                PersonId: "person-123",
                DateTime.UtcNow,
                "invocation-123"
            ),
            PolicyContext: new PolicyContext(ClientId: "test-client-1", ["scope1", "scope2"])
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
            Metadata: new SearchJobMetadata(
                PersonId: "person-123",
                DateTime.UtcNow,
                "invocation-123"
            ),
            PolicyContext: new PolicyContext(ClientId: "test-client-1", ["scope1", "scope2"])
        );

        _mockContext.GetInput<SearchOrchestratorInput>().Returns(input);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _orchestrator.RunOrchestrator(_mockContext)
        );
    }
}
