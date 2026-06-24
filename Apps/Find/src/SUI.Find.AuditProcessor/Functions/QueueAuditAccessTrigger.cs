using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Constants;
using SUI.Find.Application.Extensions;
using SUI.Find.Application.Interfaces;
using SUI.Find.Domain.Events.Audit;

namespace SUI.Find.AuditProcessor.Functions;

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
        using var activity = logger.StartActivityWithTraceParent(
            activityName: $"Handling_{nameof(AuditEvent)}",
            auditMessage.TraceParent,
            ActivityKind.Consumer,
            new Dictionary<string, object?>
            {
                ["EventName"] = auditMessage.EventName,
                ["ActorId"] = auditMessage.Actor.ActorId,
                ["TraceParent"] = auditMessage.TraceParent,
                ["InvocationId"] = context.InvocationId,
            }
        );

        logger.LogDebug(
            "Processing AuditEvent: {EventName} for ActorId: {ActorId} at {Timestamp}",
            auditMessage.EventName,
            auditMessage.Actor.ActorId,
            auditMessage.Timestamp
        );

        await auditService.WriteAccessAuditLogAsync(auditMessage, token);
    }
}
