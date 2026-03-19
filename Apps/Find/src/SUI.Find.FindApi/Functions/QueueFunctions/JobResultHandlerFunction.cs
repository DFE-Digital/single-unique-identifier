using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Models;

namespace SUI.Find.FindApi.Functions.QueueTriggerFunctions;

public class JobResultHandlerFunction(
    ILogger<JobResultHandlerFunction> logger,
    IJobResultHandler handler
)
{
    [Function(nameof(JobResultHandlerFunction))]
    public async Task Run(
        [QueueTrigger("job-results-inbound")] JobResultMessage message,
        FunctionContext context,
        CancellationToken cancellationToken
    )
    {
        using var scope = logger.BeginScope(
            new Dictionary<string, object>
            {
                ["WorkItemId"] = message.WorkItemId,
                ["JobId"] = message.JobId,
                ["SubmittingCustodianId"] = message.CustodianId,
                ["TraceParent"] = context.TraceContext.TraceParent,
                ["TraceId"] = Activity.Current?.TraceId.ToString() ?? string.Empty,
                ["InvocationId"] = context.InvocationId,
            }
        );

        await handler.HandleAsync(message, cancellationToken);
    }
}
