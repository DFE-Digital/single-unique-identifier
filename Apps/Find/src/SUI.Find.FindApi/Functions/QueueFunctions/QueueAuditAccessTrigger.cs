using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Interfaces;
using SUI.Find.Domain.Events.Audit;

namespace SUI.Find.FindApi.Functions.QueueFunctions;

public class QueueAuditAccessTrigger(
    ILogger<QueueAuditAccessTrigger> logger,
    IAuditService auditService
)
{
    [Function(nameof(QueueAuditAccessFunction))]
    public async Task QueueAuditAccessFunction(
        [QueueTrigger(ApplicationConstants.Audit.AccessQueueName)] AuditEvent auditMessage,
        FunctionContext context,
        CancellationToken token
    )
    {
        logger.LogInformation(
            "C# Queue trigger function processed: {EventType} for ClientId: {ClientId} at {Timestamp}",
            auditMessage.EventName,
            auditMessage.Actor.ActorId,
            auditMessage.Timestamp
        );

        await auditService.WriteAccessAuditLogAsync(auditMessage, token);
    }
}

public class AuditTableMapper { }
