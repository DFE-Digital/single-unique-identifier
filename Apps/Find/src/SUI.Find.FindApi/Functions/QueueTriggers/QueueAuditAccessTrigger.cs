using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Interfaces;
using SUI.Find.Domain.Models;

namespace SUI.Find.FindApi.Functions.QueueTriggers;

public class QueueAuditAccessTrigger(
    ILogger<QueueAuditAccessTrigger> logger,
    IAuditService auditService
)
{
    [Function(nameof(QueueAuditAccessFunction))]
    public async Task QueueAuditAccessFunction(
        [QueueTrigger(ApplicationConstants.Audit.AccessQueueName)] AuditAccessMessage accessMessage,
        FunctionContext context
    )
    {
        logger.LogInformation(
            "C# Queue trigger function processed: {EventType} for ClientId: {ClientId} at {Timestamp}",
            accessMessage.EventType,
            accessMessage.ClientId,
            accessMessage.Timestamp
        );
        await auditService.WriteAccessAuditLogAsync(accessMessage);

        // Queue Output messages
    }
}
