using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

// rs-todo: tests for TransferJob
public class TransferJob(
    IRecordFinder recordFinder,
    IRecordFetcher recordFetcher,
    IRecordConsolidator recordConsolidator,
    IConsolidatedDataAggregator consolidatedDataAggregator,
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<TransferJob> logger
) : ITransferJob
{
    public async Task<AggregatedConsolidatedData> TransferAsync(Guid jobId, string sui)
    {
        logger.LogInformation("Starting transfer process sui {Sui} (Job ID: {JobId})", sui, jobId);

        var cancellationToken = hostApplicationLifetime.ApplicationStopping;

        // Find records
        cancellationToken.ThrowIfCancellationRequested(); // rs-todo: test to verify this does happen
        var recordPointers = await FindRecordsAsync(sui, cancellationToken);

        // Fetch records
        cancellationToken.ThrowIfCancellationRequested();
        var unconsolidatedData = await FetchRecordsAsync(sui, recordPointers, cancellationToken);

        // Consolidate records
        cancellationToken.ThrowIfCancellationRequested();
        var consolidatedData = ConsolidateRecords(sui, unconsolidatedData);

        // Apply aggregations
        cancellationToken.ThrowIfCancellationRequested();
        return ApplyAggregations(sui, consolidatedData);
    }

    private async Task<RecordPointer[]> FindRecordsAsync(
        string sui,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("Finding records for sui {Sui}", sui);
        var recordPointers = await recordFinder.FindRecordsAsync(sui, cancellationToken);
        logger.LogInformation(
            "Found {NumOfRecordsPointers} records for sui {Sui}",
            recordPointers.Length,
            sui
        );
        return recordPointers;
    }

    private async Task<UnconsolidatedData> FetchRecordsAsync(
        string sui,
        RecordPointer[] recordPointers,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("Fetching records for sui {Sui}", sui);
        var unconsolidatedData = await recordFetcher.FetchRecordsAsync(
            sui,
            recordPointers,
            cancellationToken
        );
        logger.LogInformation(
            "Fetched {NumOfRecordsFetched} records for sui {Sui} ({NumOfRecordsFailedFetching} records failed to fetch)",
            unconsolidatedData.CountOfRecordsSuccessfullyFetched,
            sui,
            unconsolidatedData.FailedFetches.Length
        );
        return unconsolidatedData;
    }

    private ConsolidatedData ConsolidateRecords(string sui, UnconsolidatedData unconsolidatedData)
    {
        logger.LogInformation("Consolidating records for sui {Sui}", sui);
        return recordConsolidator.ConsolidateRecords(unconsolidatedData);
    }

    private AggregatedConsolidatedData ApplyAggregations(
        string sui,
        ConsolidatedData consolidatedData
    )
    {
        logger.LogInformation("Aggregating records for sui {Sui}", sui);
        return consolidatedDataAggregator.ApplyAggregations(consolidatedData);
    }
}
