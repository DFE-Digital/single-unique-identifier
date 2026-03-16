using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.FindApi.Functions.QueueFunctions;
using SUI.Find.Infrastructure.Enums;
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
    private readonly FunctionContext _mockContext;
    private readonly TraceContext _mockTraceContext;
    private readonly QueueSearchJobTrigger _trigger;

    public QueueSearchJobTriggerTests()
    {
        _mockLogger = Substitute.For<ILogger<QueueSearchJobTrigger>>();
        _mockJobRepository = Substitute.For<IJobRepository>();
        _mockWorkItemJobCountRepository = Substitute.For<IWorkItemJobCountRepository>();
        _mockCustodianService = Substitute.For<ICustodianService>();

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
            _mockCustodianService
        );
    }

    [Fact]
    public async Task QueueAuditAccessFunction_ShouldUpsertJobForEveryCustodian()
    {
        // Arrange
        var requestMessage = new SearchRequestMessage
        {
            WorkItemId = Guid.NewGuid(),
            PersonId = "test-person-id",
            RequestingCustodianId = "requesting-custodian-id",
            TraceId = "test-trace-id",
            InvocationId = "test-invocation",
        };

        var custodians = new List<ProviderDefinition>
        {
            new() { OrgId = "org1", RecordType = "type1" },
            new() { OrgId = "org2", RecordType = "type2" },
        };

        _mockCustodianService.GetCustodiansAsync().Returns(custodians);

        // Act
        await _trigger.QueueAuditAccessFunction(
            requestMessage,
            _mockContext,
            CancellationToken.None
        );

        // Assert
        await _mockJobRepository
            .Received(2)
            .UpsertAsync(
                Arg.Is<Job>(j =>
                    (j.CustodianId == "org1" || j.CustodianId == "org2")
                    && j.JobType == JobType.CustodianLookup
                    && j.WorkItemType == WorkItemType.SearchExecution
                    && j.WorkItemId == requestMessage.WorkItemId.ToString()
                    && j.JobTraceParent == "test-trace-parent"
                    && j.PayloadJson.Contains(requestMessage.PersonId)
                    && (j.PayloadJson.Contains("type1") || j.PayloadJson.Contains("type2"))
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
                    && w.ExpectedJobCount == 2
                    && w.PayloadJson.Contains(requestMessage.PersonId)
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task QueueAuditAccessFunction_ShouldNotUpsertJobWhenNoCustodians()
    {
        // Arrange
        var requestMessage = new SearchRequestMessage
        {
            WorkItemId = Guid.NewGuid(),
            PersonId = "test-person-id",
            RequestingCustodianId = "requesting-custodian-id",
            TraceId = "test-trace-id",
            InvocationId = "test-invocation",
        };

        _mockCustodianService.GetCustodiansAsync().Returns(new List<ProviderDefinition>());

        // Act
        await _trigger.QueueAuditAccessFunction(
            requestMessage,
            _mockContext,
            CancellationToken.None
        );

        // Assert
        await _mockJobRepository
            .DidNotReceive()
            .UpsertAsync(Arg.Any<Job>(), Arg.Any<CancellationToken>());

        await _mockWorkItemJobCountRepository
            .Received(1)
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
}
