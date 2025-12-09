using System.Diagnostics.CodeAnalysis;
using Azure.Data.Tables;
using SUI.Find.Application.Interfaces;
using SUI.Find.Domain.Models;
using SUI.Find.Infrastructure.Interfaces;

namespace SUI.Find.Infrastructure.Services;

[ExcludeFromCodeCoverage(Justification = "Basic Infrastructure code.")]
public class AuditStorageTableService(TableServiceClient client)
    : IAuditService,
        ITableServiceEnsureCreated
{
    public async Task WriteAccessAuditLogAsync(AuditAccessMessage accessMessage)
    {
        await WriteAsync(accessMessage);
    }

    private async Task WriteAsync(AuditAccessMessage accessMessage)
    {
        var tableClient = client.GetTableClient(
            InfrastructureConstants.StorageTableAudit.TableName
        );

        // Partition by clientId to avoid hot partitions, and by month for easier querying
        var partitionKey = $"{accessMessage.ClientId}_{accessMessage.Timestamp:yyyyMM}";
        await tableClient.AddEntityAsync(
            new TableEntity(partitionKey, Guid.NewGuid().ToString())
            {
                { "ClientId", accessMessage.ClientId },
                { "UrlAccessed", accessMessage.Path },
                { "Method", accessMessage.Method },
                { "AccessTime", accessMessage.Timestamp.ToUniversalTime() },
                { "CorrelationId", accessMessage.CorrelationId },
                { "Suid", accessMessage.Suid },
            }
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
