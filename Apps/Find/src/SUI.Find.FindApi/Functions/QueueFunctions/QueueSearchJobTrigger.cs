using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Constants;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Models;

namespace SUI.Find.FindApi.Functions.QueueFunctions;

public class QueueSearchJobTrigger(
    ILogger<QueueSearchJobTrigger> logger,
    IJobQueueService jobQueueService
)
{
    [Function(nameof(QueueAuditAccessFunction))]
    public async Task QueueAuditAccessFunction(
        [QueueTrigger(ApplicationConstants.SearchJobs.QueueName)]
            SearchRequestMessage searchRequestMessage,
        FunctionContext context,
        CancellationToken token
    )
    {
        using var scope = logger.BeginScope(
            new Dictionary<string, object>
            {
                { "WorkItemId", searchRequestMessage.WorkItemId },
                { "PersonId", searchRequestMessage.PersonId },
                { "RequestingCustodianId", searchRequestMessage.RequestingCustodianId },
                { "TraceParent", context.TraceContext.TraceParent },
                { "TraceId", context.TraceContext.TraceState },
                { "InvocationId", context.InvocationId },
            }
        );
        logger.LogInformation(
            "QueueSearchJobTrigger function processed: Work item ID:{WorkItemId} for Custodian ID: {CustodianId}",
            searchRequestMessage.WorkItemId,
            searchRequestMessage.RequestingCustodianId
        );
    }
}
