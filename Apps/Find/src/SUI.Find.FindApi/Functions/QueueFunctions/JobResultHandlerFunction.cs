using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Extensions;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;

namespace SUI.Find.FindApi.Functions.QueueFunctions;

public class JobResultHandlerFunction(
    ILogger<JobResultHandlerFunction> logger,
    IJobResultHandler handler
)
{
    [Function(nameof(JobResultHandlerFunction))]
    public async Task Run(
        [QueueTrigger(ApplicationConstants.Jobs.JobResultsQueueName)] JobResultMessage message,
        FunctionContext context,
        CancellationToken cancellationToken
    )
    {
        using var activity = logger.StartActivityWithTraceParent(
            activityName: $"Handling_{nameof(JobResultMessage)}",
            message.JobTraceParent,
            new Dictionary<string, object?>
            {
                ["WorkItemId"] = message.WorkItemId,
                ["JobId"] = message.JobId,
                ["SubmittingCustodianId"] = message.CustodianId,
                ["TraceParent"] = message.JobTraceParent,
                ["TraceId"] = Activity.Current?.TraceId.ToString() ?? string.Empty,
                ["InvocationId"] = context.InvocationId,
            }
        );

        logger.LogInformation(
            "Processing JobResultMessage for JobId {JobId} and WorkItemId {WorkItemId}",
            message.JobId,
            message.WorkItemId
        );

        await handler.HandleAsync(message, context.InvocationId, cancellationToken);
    }
}
