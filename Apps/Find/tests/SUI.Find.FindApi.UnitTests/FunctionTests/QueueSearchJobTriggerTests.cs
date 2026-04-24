using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Enums;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.Application.Models.Pep;
using SUI.Find.FindApi.Functions.QueueFunctions;
using SUI.Find.Infrastructure.Models;
using SUI.Find.Infrastructure.Repositories.JobRepository;
using SUI.Find.Infrastructure.Repositories.WorkItemJobCountRepository;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class QueueSearchJobTriggerTests
{
    private readonly ILogger<QueueSearchJobTrigger> _mockLogger;
    private readonly IJobRepository _mockJobRepository;
    private readonly IWorkItemJobCountRepository _mockWorkItemJobCountRepository;
    private readonly ICustodianService _mockCustodianService;
    private readonly IPolicyEnforcementService _mockPolicyEnforcementService;
    private readonly FunctionContext _mockContext;
    private readonly TraceContext _mockTraceContext;
    private readonly QueueSearchJobTrigger _trigger;

    public QueueSearchJobTriggerTests()
    {
        _mockLogger = Substitute.For<ILogger<QueueSearchJobTrigger>>();
        _mockJobRepository = Substitute.For<IJobRepository>();
        _mockWorkItemJobCountRepository = Substitute.For<IWorkItemJobCountRepository>();
        _mockCustodianService = Substitute.For<ICustodianService>();
        _mockPolicyEnforcementService = Substitute.For<IPolicyEnforcementService>();

        _mockContext = Substitute.For<FunctionContext>();
        _mockTraceContext = Substitute.For<TraceContext>();
        _mockTraceContext.TraceParent.Returns("test-trace-parent");
        _mockTraceContext.TraceState.Returns("test-trace-state");
        _mockContext.TraceContext.Returns(_mockTraceContext);
        _mockContext.InvocationId.Returns("test-invocation-id");

        _trigger = new QueueSearchJobTrigger(
            _mockLogger,
            _mockJobRepository,
            _mockWorkItemJobCountRepository,
            _mockCustodianService,
            _mockPolicyEnforcementService
        );
    }

    [Fact]
    public async Task QueueSearchJobFunction_ShouldUpsertJobForEveryAllowedCustodian()
    {
        // Arrange
        var requestMessage = new SearchRequestMessage
        {
            WorkItemId = Guid.NewGuid(),
            PersonId = "test-person-id",
            SearchingOrganisationId = "searching-custodian-id",
            TraceId = "test-trace-id",
            InvocationId = "test-invocation",
            TraceParent = "original-search-request-trace-parent",
        };

        var custodians = new List<ProviderDefinition>
        {
            new()
            {
                OrgId = "searching-custodian-id",
                OrgType = "typeA",
                RecordType = "type0",
            },
            new() { OrgId = "org1", RecordType = "type1" },
            new() { OrgId = "org2", RecordType = "type2" },
        };

        _mockCustodianService.GetCustodiansAsync().Returns(custodians);

        _mockPolicyEnforcementService
            .EvaluateAsync(
                Arg.Any<PolicyDecisionRequest>(),
                Arg.Any<DsaPolicyDefinition>(),
                Arg.Any<string>()
            )
            .Returns(
                Task.FromResult(new PolicyDecisionResult { IsAllowed = true, Reason = "Allowed" })
            );

        // Act
        await _trigger.QueueSearchJobFunction(requestMessage, _mockContext, CancellationToken.None);

        // Assert
        await _mockJobRepository
            .Received(3)
            .UpsertAsync(
                Arg.Is<Job>(j =>
                    (
                        j.CustodianId == "org1"
                        || j.CustodianId == "org2"
                        || j.CustodianId == "searching-custodian-id"
                    )
                    && j.SearchingOrganisationId == requestMessage.SearchingOrganisationId
                    && j.JobType == JobType.CustodianLookup
                    && j.WorkItemType == WorkItemType.SearchExecution
                    && j.WorkItemId == requestMessage.WorkItemId.ToString()
                    && j.JobTraceParent == "original-search-request-trace-parent"
                    && j.PayloadJson.Contains(requestMessage.PersonId)
                ),
                Arg.Any<CancellationToken>()
            );

        await _mockJobRepository
            .Received(1)
            .UpsertAsync(
                Arg.Is<Job>(j => j.CustodianId == "org1" && j.PayloadJson.Contains("type1")),
                Arg.Any<CancellationToken>()
            );

        await _mockJobRepository
            .Received(1)
            .UpsertAsync(
                Arg.Is<Job>(j => j.CustodianId == "org2" && j.PayloadJson.Contains("type2")),
                Arg.Any<CancellationToken>()
            );

        await _mockWorkItemJobCountRepository
            .Received(1)
            .UpsertAsync(
                Arg.Is<WorkItemJobCount>(w =>
                    w.JobType == JobType.CustodianLookup
                    && w.WorkItemId == requestMessage.WorkItemId.ToString()
                    && w.ExpectedJobCount == 3
                    && w.PayloadJson.Contains(requestMessage.PersonId)
                    && w.SearchingOrganisationId == requestMessage.SearchingOrganisationId
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task QueueSearchJobFunction_ShouldNotUpsertJobWhenNoCustodians()
    {
        // Arrange
        var requestMessage = new SearchRequestMessage
        {
            WorkItemId = Guid.NewGuid(),
            PersonId = "test-person-id",
            SearchingOrganisationId = "searching-custodian-id",
            TraceId = "test-trace-id",
            InvocationId = "test-invocation",
        };

        var custodians = new List<ProviderDefinition>
        {
            new() { OrgId = "searching-custodian-id" },
        };

        _mockCustodianService.GetCustodiansAsync().Returns(custodians);
        _mockPolicyEnforcementService
            .EvaluateAsync(
                Arg.Any<PolicyDecisionRequest>(),
                Arg.Any<DsaPolicyDefinition>(),
                Arg.Any<string>()
            )
            .Returns(
                Task.FromResult(new PolicyDecisionResult { IsAllowed = false, Reason = "Denied" })
            );

        // Act
        await _trigger.QueueSearchJobFunction(requestMessage, _mockContext, CancellationToken.None);

        // Assert
        await _mockJobRepository
            .DidNotReceive()
            .UpsertAsync(Arg.Any<Job>(), Arg.Any<CancellationToken>());

        await _mockWorkItemJobCountRepository
            .DidNotReceive()
            .UpsertAsync(
                Arg.Is<WorkItemJobCount>(w =>
                    w.JobType == JobType.CustodianLookup
                    && w.WorkItemId == requestMessage.WorkItemId.ToString()
                    && w.ExpectedJobCount == 0
                    && w.PayloadJson.Contains(requestMessage.PersonId)
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task QueueSearchJobFunction_ShouldSkipJobsWhenPepDenies()
    {
        // Arrange
        var requestMessage = new SearchRequestMessage
        {
            WorkItemId = Guid.NewGuid(),
            PersonId = "test-person-id",
            SearchingOrganisationId = "searching-custodian-id",
            TraceId = "test-trace-id",
            InvocationId = "test-invocation",
            TraceParent = "original-search-request-trace-parent",
        };

        var custodians = new List<ProviderDefinition>
        {
            new()
            {
                OrgId = "searching-custodian-id",
                OrgType = "typeA",
                RecordType = "type0",
            },
            new() { OrgId = "org1", RecordType = "type1" },
            new() { OrgId = "org2", RecordType = "type2" },
        };

        _mockCustodianService.GetCustodiansAsync().Returns(custodians);

        _mockPolicyEnforcementService
            .EvaluateAsync(
                Arg.Is<PolicyDecisionRequest>(req => req.SourceOrgId == "org1"),
                Arg.Any<DsaPolicyDefinition>(),
                Arg.Any<string>()
            )
            .Returns(
                Task.FromResult(new PolicyDecisionResult { IsAllowed = false, Reason = "Denied" })
            );

        _mockPolicyEnforcementService
            .EvaluateAsync(
                Arg.Is<PolicyDecisionRequest>(req => req.SourceOrgId != "org1"),
                Arg.Any<DsaPolicyDefinition>(),
                Arg.Any<string>()
            )
            .Returns(
                Task.FromResult(new PolicyDecisionResult { IsAllowed = true, Reason = "Allowed" })
            );

        // Act
        await _trigger.QueueSearchJobFunction(requestMessage, _mockContext, CancellationToken.None);

        // Assert
        // Should only upsert for searching-custodian-id and org2
        await _mockJobRepository
            .Received(2)
            .UpsertAsync(Arg.Any<Job>(), Arg.Any<CancellationToken>());

        await _mockJobRepository
            .DidNotReceive()
            .UpsertAsync(Arg.Is<Job>(j => j.CustodianId == "org1"), Arg.Any<CancellationToken>());

        await _mockJobRepository
            .Received(1)
            .UpsertAsync(Arg.Is<Job>(j => j.CustodianId == "org2"), Arg.Any<CancellationToken>());

        await _mockWorkItemJobCountRepository
            .Received(1)
            .UpsertAsync(
                Arg.Is<WorkItemJobCount>(w =>
                    w.JobType == JobType.CustodianLookup
                    && w.WorkItemId == requestMessage.WorkItemId.ToString()
                    && w.ExpectedJobCount == 2
                    && w.PayloadJson.Contains(requestMessage.PersonId)
                    && w.SearchingOrganisationId == requestMessage.SearchingOrganisationId
                ),
                Arg.Any<CancellationToken>()
            );
    }
}
