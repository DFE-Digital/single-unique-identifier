using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using SUI.Find.Infrastructure.Enums;
using SUI.Find.Infrastructure.Interfaces;

namespace SUI.Find.Infrastructure.Repositories.JobRepository;

public class JobRepository : IJobRepository, ITableServiceEnsureCreated
{
    private readonly TableServiceClient _client;
    private readonly ILogger<JobRepository> _logger;

    private const string TableName = InfrastructureConstants.StorageTableJobRepository.TableName;

    public JobRepository(TableServiceClient client, ILogger<JobRepository> logger)
    {
        _client = client;
        _logger = logger;
    }

    private TableClient Table => _client.GetTableClient(TableName);

    public async Task UpsertAsync(Job job, CancellationToken cancellationToken = default)
    {
        var partitionKey = JobKeys.PartitionKey(job.CustodianId);
        var rowKey = JobKeys.RowKey(job.CreatedAtUtc, job.JobId);

        var entity = new TableEntity(partitionKey, rowKey)
        {
            { "JobId", job.JobId },
            { "CustodianId", job.CustodianId },
            { "JobType", job.JobType.ToString() },
            { "WorkItemType", job.WorkItemType.ToString() },
            { "WorkItemId", job.WorkItemId },
            { "LeaseId", job.LeaseId },
            { "LeaseExpiresAtUtc", job.LeaseExpiresAtUtc },
            { "AttemptCount", job.AttemptCount },
            { "CreatedAtUtc", job.CreatedAtUtc },
            { "UpdatedAtUtc", job.UpdatedAtUtc },
            { "CompletedAtUtc", job.CompletedAtUtc },
            { "PayloadJson", job.PayloadJson },
            { "JobTraceParent", job.JobTraceParent },
        };

        try
        {
            await Table.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to upsert Job {JobId} for Custodian {CustodianId}",
                job.JobId,
                job.CustodianId
            );
        }
    }

    public async Task<IReadOnlyList<Job>> ListJobsByCustodianIdAsync(
        string custodianId,
        DateTimeOffset windowStart,
        CancellationToken cancellationToken = default
    )
    {
        var partitionKey = JobKeys.PartitionKey(custodianId);
        var windowStartRowKey = JobKeys.RowKey(windowStart, "default");

        var results = new List<Job>();

        await foreach (
            var entity in Table.QueryAsync<TableEntity>(
                x => x.PartitionKey == partitionKey && x.RowKey.CompareTo(windowStartRowKey) >= 0,
                cancellationToken: cancellationToken
            )
        )
        {
            try
            {
                var jobTypeString = entity.GetString("JobType");

                if (!Enum.TryParse<JobType>(jobTypeString, out var jobType))
                {
                    jobType = JobType.Unknown;
                }

                var workItemTypeString = entity.GetString("WorkItemType");

                if (!Enum.TryParse<WorkItemType>(workItemTypeString, out var workItemType))
                {
                    workItemType = WorkItemType.Unknown;
                }

                results.Add(
                    new Job
                    {
                        JobId = entity.GetString("JobId"),
                        CustodianId = entity.GetString("CustodianId"),
                        JobType = jobType,
                        WorkItemType = workItemType,
                        WorkItemId = entity.GetString("WorkItemId"),
                        LeaseId = entity.GetString("LeaseId"),
                        LeaseExpiresAtUtc = entity.GetDateTimeOffset("LeaseExpiresAtUtc"),
                        AttemptCount = entity.GetInt32("AttemptCount") ?? 0,
                        CreatedAtUtc = entity.GetDateTimeOffset("CreatedAtUtc")!.Value,
                        UpdatedAtUtc = entity.GetDateTimeOffset("UpdatedAtUtc")!.Value,
                        CompletedAtUtc = entity.GetDateTimeOffset("CompletedAtUtc"),
                        PayloadJson = entity.GetString("PayloadJson"),
                        JobTraceParent = entity.GetString("JobTraceParent"),
                        ETag = entity.ETag.ToString(),
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to map Job row {PartitionKey}/{RowKey}",
                    entity.PartitionKey,
                    entity.RowKey
                );
            }
        }

        return results;
    }

    public async Task EnsureTableExistsAsync(CancellationToken cancellationToken)
    {
        await _client.CreateTableIfNotExistsAsync(TableName, cancellationToken);
    }
}
