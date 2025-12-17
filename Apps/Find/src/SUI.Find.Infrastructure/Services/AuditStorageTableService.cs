using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Azure.Data.Tables;
using SUI.Find.Application.Interfaces;
using SUI.Find.Domain.Events.Audit;
using SUI.Find.Infrastructure.Interfaces;

namespace SUI.Find.Infrastructure.Services;

[ExcludeFromCodeCoverage(Justification = "Basic Infrastructure code.")]
public class AuditStorageTableService(TableServiceClient client)
    : IAuditService,
        ITableServiceEnsureCreated
{
    public async Task WriteAccessAuditLogAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken
    )
    {
        var partitionKey = $"{auditEvent.Timestamp:yyyy-MM-dd}";

        var tableClient = client.GetTableClient(
            InfrastructureConstants.StorageTableAudit.TableName
        );

        await tableClient.AddEntityAsync(
            new TableEntity(partitionKey, auditEvent.EventId)
            {
                { "EventId", auditEvent.EventId },
                { "EventName", auditEvent.EventName },
                { "ServiceName", auditEvent.ServiceName },
                { "ActorId", auditEvent.Actor.ActorId },
                { "ActorRole", auditEvent.Actor.ActorRole },
                { "Timestamp", auditEvent.Timestamp.ToUniversalTime() },
                { "CorrelationId", auditEvent.CorrelationId },
                { "Payload", auditEvent.Payload.ToString() },
            },
            cancellationToken
        );
    }

    public async Task EnsureAuditTableExistsAsync(CancellationToken cancellationToken)
    {
        await client.CreateTableIfNotExistsAsync(
            InfrastructureConstants.StorageTableAudit.TableName,
            cancellationToken
        );
    }
}
