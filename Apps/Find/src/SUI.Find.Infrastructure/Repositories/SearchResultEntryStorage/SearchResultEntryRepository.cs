using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Dtos;
using SUI.Find.Application.Interfaces;
using SUI.Find.Infrastructure.Interfaces;

namespace SUI.Find.Infrastructure.Repositories.SearchResultEntryStorage;

public class SearchResultEntryRepository : ISearchResultEntryRepository, ITableServiceEnsureCreated
{
    private readonly TableServiceClient _client;
    private readonly ILogger<SearchResultEntryRepository> _logger;

    private const string TableName = InfrastructureConstants
        .StorageTableSearchResultEntries
        .TableName;

    public SearchResultEntryRepository(
        TableServiceClient client,
        ILogger<SearchResultEntryRepository> logger
    )
    {
        _client = client;
        _logger = logger;
    }

    private TableClient Table => _client.GetTableClient(TableName);

    public async Task UpsertAsync(
        SearchResultEntry entry,
        CancellationToken cancellationToken = default
    )
    {
        var partitionKey = SearchResultEntryKeys.PartitionKey(entry.WorkItemId);

        var rowKey = SearchResultEntryKeys.RowKey(
            entry.SubmittedAtUtc,
            entry.CustodianId,
            entry.RecordType,
            entry.SystemId
        );

        var entity = new TableEntity(partitionKey, rowKey)
        {
            { "CustodianId", entry.CustodianId },
            { "SystemId", entry.SystemId },
            { "RecordType", entry.RecordType },
            { "RecordUrl", entry.RecordUrl },
            { "RecordId", entry.RecordId },
            { "SubmittedAtUtc", entry.SubmittedAtUtc },
            { "JobId", entry.JobId },
            { "WorkItemId", entry.WorkItemId },
        };

        try
        {
            // Duplicate-safe
            await Table.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to persist SearchResult for WorkItemId {WorkItemId}",
                entry.WorkItemId
            );
        }
    }

    public async Task<IReadOnlyList<SearchResultEntry>> GetByWorkItemIdAsync(
        string workItemId,
        CancellationToken cancellationToken = default
    )
    {
        var partitionKey = SearchResultEntryKeys.PartitionKey(workItemId);

        var results = new List<SearchResultEntry>();
        var seen = new HashSet<string>();

        await foreach (
            var entity in Table.QueryAsync<TableEntity>(
                x => x.PartitionKey == partitionKey,
                cancellationToken: cancellationToken
            )
        )
        {
            try
            {
                var custodianId = entity.GetString("CustodianId");
                var systemId = entity.GetString("SystemId");
                var recordType = entity.GetString("RecordType");

                var dedupeKey = $"{custodianId}|{systemId}|{recordType}";

                if (seen.Contains(dedupeKey))
                {
                    continue; // skip duplicate
                }

                seen.Add(dedupeKey);

                results.Add(
                    new SearchResultEntry
                    {
                        CustodianId = custodianId,
                        SystemId = systemId,
                        RecordType = recordType,
                        RecordUrl = entity.GetString("RecordUrl"),
                        RecordId = entity.GetString("RecordId"),
                        SubmittedAtUtc = entity
                            .GetDateTimeOffset("SubmittedAtUtc")!
                            .Value.UtcDateTime,
                        JobId = entity.GetString("JobId"),
                        WorkItemId = entity.GetString("WorkItemId"),
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to map SearchResult row {PartitionKey}/{RowKey}",
                    entity.PartitionKey,
                    entity.RowKey
                );
            }
        }

        // IMPORTANT:
        // Do NOT re-order by RecordType.
        // Azure Tables already return ordered by PartitionKey + RowKey.
        // Since RowKey starts with ticks, this is chronological.

        return results;
    }

    public async Task EnsureTableExistsAsync(CancellationToken cancellationToken)
    {
        await _client.CreateTableIfNotExistsAsync(TableName, cancellationToken);
    }
}
