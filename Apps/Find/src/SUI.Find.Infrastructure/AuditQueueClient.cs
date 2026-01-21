using System.Text.Json;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Interfaces;
using SUI.Find.Domain.Events.Audit;
using SUI.Find.Infrastructure.Factories;

namespace SUI.Find.Infrastructure;

public class AuditQueueClient(
    ILogger<AuditQueueClient> logger,
    IQueueClientFactory queueClientFactory
) : IAuditQueueClient
{
    public async Task SendMessageAsync(AuditEvent auditMessage, CancellationToken cancellationToken)
    {
        var queueClient = queueClientFactory.GetAuditClient();
        var messageJson = JsonSerializer.Serialize(auditMessage, JsonSerializerOptions.Web);
        var auditMessageBytes = System.Text.Encoding.UTF8.GetBytes(messageJson);
        var base64Message = Convert.ToBase64String(auditMessageBytes);
        try
        {
            await queueClient.SendMessageAsync(base64Message, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to send audit message for: {Actor} CorrelationId: {CorrelationId}, Service {ServiceName}",
                auditMessage.Actor,
                auditMessage.CorrelationId,
                auditMessage.ServiceName
            );
        }
    }
}
