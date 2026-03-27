using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;
using SUI.Find.FindApi.Functions.QueueFunctions;

namespace SUI.Find.FindApi.UnitTests.FunctionTests;

public class JobResultHandlerFunctionTests
{
    private readonly ILogger<JobResultHandlerFunction> _logger = Substitute.For<
        ILogger<JobResultHandlerFunction>
    >();

    private readonly IJobResultHandler _handler = Substitute.For<IJobResultHandler>();

    private readonly JobResultHandlerFunction _function;

    public JobResultHandlerFunctionTests()
    {
        _function = new JobResultHandlerFunction(_logger, _handler);
    }

    [Fact]
    public async Task Run_ShouldInvokeHandler_WithCorrectMessage()
    {
        // Arrange
        var message = new JobResultMessage
        {
            JobId = "job-123",
            WorkItemId = "work-123",
            CustodianId = "cust-1",
            LeaseId = "lease-1",
            SubmittedAtUtc = DateTimeOffset.UtcNow,
            JobType = Application.Enums.JobType.CustodianLookup,
            Records = [],
        };

        var context = CreateFunctionContext();

        // Act
        await _function.Run(message, context, CancellationToken.None);

        // Assert
        await _handler
            .Received(1)
            .HandleAsync(
                Arg.Is<JobResultMessage>(m =>
                    m.JobId == "job-123" && m.WorkItemId == "work-123" && m.CustodianId == "cust-1"
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task Run_ShouldPassCancellationToken()
    {
        // Arrange
        var message = new JobResultMessage
        {
            JobId = "job-123",
            WorkItemId = "work-123",
            CustodianId = "cust-1",
            LeaseId = "lease-1",
            SubmittedAtUtc = DateTimeOffset.UtcNow,
            JobType = Application.Enums.JobType.CustodianLookup,
            Records = [],
        };

        var context = CreateFunctionContext();
        var cts = new CancellationTokenSource();

        // Act
        await _function.Run(message, context, cts.Token);

        // Assert
        await _handler
            .Received(1)
            .HandleAsync(
                Arg.Any<JobResultMessage>(),
                Arg.Is<CancellationToken>(t => t == cts.Token)
            );
    }

    // Helpers
    private static FunctionContext CreateFunctionContext()
    {
        var context = Substitute.For<FunctionContext>();

        context.TraceContext.Returns(Substitute.For<TraceContext>());
        context.InvocationId.Returns("test-invocation-id");

        return context;
    }
}
