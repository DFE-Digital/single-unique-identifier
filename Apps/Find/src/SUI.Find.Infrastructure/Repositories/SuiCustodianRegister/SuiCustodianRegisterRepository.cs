using System.Diagnostics.CodeAnalysis;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using SUI.Find.Infrastructure.Interfaces;

namespace SUI.Find.Infrastructure.Repositories.SuiCustodianRegister;

[ExcludeFromCodeCoverage(Justification = "Basic Infrastructure code.")]
public class SuiCustodianRegisterRepository : IIdRegisterRepository, ITableServiceEnsureCreated
{
    private readonly TableServiceClient _client;
    private readonly ILogger<SuiCustodianRegisterRepository> _logger;

    private const string TableName = InfrastructureConstants.StorageTableIdRegister.TableName;

    public SuiCustodianRegisterRepository(
        TableServiceClient client,
        ILogger<SuiCustodianRegisterRepository> logger
    )
    {
        _client = client;
        _logger = logger;
    }

    private TableClient Table => _client.GetTableClient(TableName);

    public async Task UpsertAsync(
        IdRegisterEntry registerEntry,
        CancellationToken cancellationToken = default
    )
    {
        var partitionKey = RegisterKeys.PartitionKey(registerEntry.Sui);

        var systemId = string.IsNullOrWhiteSpace(registerEntry.SystemId)
            ? "DefaultSystem"
            : registerEntry.SystemId;

        var rowKey = RegisterKeys.RowKey(
            registerEntry.CustodianId,
            registerEntry.RecordType,
            systemId
        );

        TableEntity entity;

        try
        {
            // Existing row
            var existing = await Table.GetEntityAsync<TableEntity>(
                partitionKey,
                rowKey,
                cancellationToken: cancellationToken
            );

            entity = existing.Value;

            // Flow A & C: always update LastSeen
            entity["LastSeenUtc"] = DateTimeOffset.UtcNow;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // New row
            entity = new TableEntity(partitionKey, rowKey)
            {
                { "FirstSeenUtc", DateTimeOffset.UtcNow },
                { "LastSeenUtc", DateTimeOffset.UtcNow },
            };
        }

        // Factual identity fields (always overwritten)
        entity["Sui"] = registerEntry.Sui;
        entity["CustodianId"] = registerEntry.CustodianId;
        entity["RecordType"] = registerEntry.RecordType;
        entity["SystemId"] = systemId;
        entity["CustodianSubjectId"] = registerEntry.CustodianSubjectId;
        entity["Provenance"] = registerEntry.Provenance.ToString();

        // Distributed ID handling
        if (registerEntry.LastIdDeliveredAtUtc.HasValue)
        {
            // Flow A: issuance or refresh
            entity["LastIdDeliveredUtc"] = registerEntry.LastIdDeliveredAtUtc.Value;
        }
        else if (registerEntry.Provenance == Provenance.DiscoveredViaFanout)
        {
            // Flow C: fan-out must NOT imply issuance
            entity.Remove("LastIdDeliveredUtc");
        }

        await Table.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
    }

    public async Task<IReadOnlyList<IdRegisterEntry>> GetEntriesBySuiAsync(
        string sui,
        CancellationToken cancellationToken = default
    )
    {
        var partitionKey = RegisterKeys.PartitionKey(sui);
        var results = new List<IdRegisterEntry>();

        await foreach (
            var e in Table.QueryAsync<TableEntity>(
                x => x.PartitionKey == partitionKey,
                cancellationToken: cancellationToken
            )
        )
        {
            try
            {
                var provenanceString = e.GetString("Provenance");

                if (!Enum.TryParse<Provenance>(provenanceString, out var provenance))
                {
                    provenance = Provenance.Unknown;
                }

                results.Add(
                    new IdRegisterEntry
                    {
                        Sui = e.GetString("Sui"),
                        CustodianId = e.GetString("CustodianId"),
                        RecordType = e.GetString("RecordType"),
                        SystemId = e.GetString("SystemId"),
                        CustodianSubjectId = e.GetString("CustodianSubjectId"),
                        FirstSeenUtc = e.GetDateTimeOffset("FirstSeenUtc")!.Value,
                        LastSeenUtc = e.GetDateTimeOffset("LastSeenUtc")!.Value,
                        Provenance = provenance,
                        LastIdDeliveredAtUtc = e.GetDateTimeOffset("LastIdDeliveredUtc"),
                    }
                );
            }
            catch (Exception ex)
            {
                // log and skip bad row
                _logger.LogWarning(
                    ex,
                    "Failed to map ID Register entry {PartitionKey}/{RowKey}",
                    e.PartitionKey,
                    e.RowKey
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
