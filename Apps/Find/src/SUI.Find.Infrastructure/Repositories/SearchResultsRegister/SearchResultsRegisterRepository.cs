using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Models;
using SUI.Find.Infrastructure.Interfaces;

namespace SUI.Find.Infrastructure.Repositories.SearchResultsRegister;

public class SearchResultsRegisterRepository
    : ISearchResultsRegisterRepository,
        ITableServiceEnsureCreated
{
    private readonly TableServiceClient _client;
    private readonly ILogger<SearchResultsRegisterRepository> _logger;

    private const string TableName = InfrastructureConstants
        .StorageTableSearchResultsRegister
        .TableName;

    public SearchResultsRegisterRepository(
        TableServiceClient client,
        ILogger<SearchResultsRegisterRepository> logger
    )
    {
        _client = client;
        _logger = logger;
    }

    private TableClient Table => _client.GetTableClient(TableName);

    public async Task AddAsync(
        string jobId,
        CustodianSearchResultItem item,
        CancellationToken cancellationToken = default
    )
    {
        var partitionKey = RegisterKeys.PartitionKey(jobId);

        var submittedAtUtc = DateTimeOffset.UtcNow;

        var systemId = string.IsNullOrWhiteSpace(item.SystemId) ? "DefaultSystem" : item.SystemId;

        var rowKey = RegisterKeys.RowKey(
            submittedAtUtc,
            item.CustodianId,
            item.RecordType,
            systemId
        );

        var entity = new TableEntity(partitionKey, rowKey)
        {
            { "CustodianId", item.CustodianId },
            { "SystemId", systemId },
            { "RecordType", item.RecordType },
            { "RecordUrl", item.RecordUrl },
            { "SubmittedAtUtc", submittedAtUtc },
            { "JobId", jobId },
        };

        try
        {
            // Duplicate-safe
            await Table.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist SearchResult for JobId {JobId}", jobId);
        }
    }

    public async Task<IReadOnlyList<SearchResultsRegisterEntry>> GetByJobIdAsync(
        string jobId,
        CancellationToken cancellationToken = default
    )
    {
        var partitionKey = RegisterKeys.PartitionKey(jobId);

        var results = new List<SearchResultsRegisterEntry>();
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
                    new SearchResultsRegisterEntry
                    {
                        CustodianId = custodianId,
                        SystemId = systemId,
                        RecordType = recordType,
                        RecordUrl = entity.GetString("RecordUrl"),
                        SubmittedAtUtc = entity
                            .GetDateTimeOffset("SubmittedAtUtc")!
                            .Value.UtcDateTime,
                        JobId = entity.GetString("JobId"),
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
