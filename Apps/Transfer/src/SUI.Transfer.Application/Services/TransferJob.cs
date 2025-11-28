using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public class TransferJob(
    IRecordFinder recordFinder,
    IRecordFetcher recordFetcher,
    IRecordConsolidator recordConsolidator,
    IConsolidatedDataAggregator consolidatedDataAggregator,
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<TransferJob> logger
) : ITransferJob
{
    public async Task<AggregatedData> TransferAsync(Guid jobId, string sui)
    {
        logger.LogDebug("Starting transfer process sui {Sui} (Job ID: {JobId})", sui, jobId);

        var cancellationToken = hostApplicationLifetime.ApplicationStopping;

        // Find records
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogDebug("Finding records for sui {Sui}", sui);
        var recordPointers = await recordFinder.FindRecordsAsync(sui, cancellationToken);
        logger.LogInformation(
            "Found {NumOfRecordsPointers} records for sui {Sui}",
            recordPointers.Length,
            sui
        );

        // Fetch records
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogDebug("Fetching records for sui {Sui}", sui);
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

        // Consolidate records
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogDebug("Consolidating records for sui {Sui}", sui);
        var consolidatedData = recordConsolidator.ConsolidateRecords(unconsolidatedData);

        // Apply aggregations
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogDebug("Aggregating records for sui {Sui}", sui);
        return consolidatedDataAggregator.ApplyAggregations(consolidatedData);
    }
}
