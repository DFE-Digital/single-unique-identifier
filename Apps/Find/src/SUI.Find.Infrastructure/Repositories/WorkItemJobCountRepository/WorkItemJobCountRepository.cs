using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using SUI.Find.Infrastructure.Enums;
using SUI.Find.Infrastructure.Interfaces;

namespace SUI.Find.Infrastructure.Repositories.WorkItemJobCountRepository;

public class WorkItemJobCountRepository : IWorkItemJobCountRepository, ITableServiceEnsureCreated
{
    private readonly TableServiceClient _client;
    private readonly ILogger<WorkItemJobCountRepository> _logger;

    private const string TableName = InfrastructureConstants
        .StorageTableWorkItemJobCountRepository
        .TableName;

    public WorkItemJobCountRepository(
        TableServiceClient client,
        ILogger<WorkItemJobCountRepository> logger
    )
    {
        _client = client;
        _logger = logger;
    }

    private TableClient Table => _client.GetTableClient(TableName);

    public async Task UpsertAsync(
        WorkItemJobCount workItemJobCount,
        CancellationToken cancellationToken = default
    )
    {
        var partitionKey = WorkItemJobCountKeys.PartitionKey(workItemJobCount.WorkItemId);
        var rowKey = WorkItemJobCountKeys.RowKey(workItemJobCount.JobType);

        var entity = new TableEntity(partitionKey, rowKey)
        {
            { "WorkItemId", workItemJobCount.WorkItemId },
            { "JobType", workItemJobCount.JobType.ToString() },
            { "ExpectedJobCount", workItemJobCount.ExpectedJobCount },
            { "CreatedAtUtc", workItemJobCount.CreatedAtUtc },
            { "UpdatedAtUtc", workItemJobCount.UpdatedAtUtc },
            { "PayloadJson", workItemJobCount.PayloadJson },
        };

        try
        {
            await Table.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to upsert WorkItemJobCount {WorkItemId}/{JobType}",
                workItemJobCount.WorkItemId,
                workItemJobCount.JobType
            );
        }
    }

    public async Task<int?> GetByWorkItemIdAndJobTypeAsync(
        string workItemId,
        JobType jobType,
        CancellationToken cancellationToken = default
    )
    {
        var partitionKey = WorkItemJobCountKeys.PartitionKey(workItemId);
        var rowKey = WorkItemJobCountKeys.RowKey(jobType);

        try
        {
            var response = await Table.GetEntityIfExistsAsync<TableEntity>(
                partitionKey,
                rowKey,
                cancellationToken: cancellationToken
            );

            if (!response.HasValue)
            {
                return null;
            }

            return response.Value!.GetInt32("ExpectedJobCount");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to retrieve WorkItemJobCount {WorkItemId}/{JobType}",
                workItemId,
                jobType
            );

            return null;
        }
    }

    public async Task EnsureTableExistsAsync(CancellationToken cancellationToken)
    {
        await _client.CreateTableIfNotExistsAsync(TableName, cancellationToken);
    }
}
