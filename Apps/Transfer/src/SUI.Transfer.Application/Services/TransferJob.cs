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
        logger.LogInformation("Starting transfer process sui {sui} (Job ID: {jobId})", sui, jobId);

        var cancellationToken = hostApplicationLifetime.ApplicationStopping;

        // Find records
        cancellationToken.ThrowIfCancellationRequested(); // rs-todo: test to verify this does happen
        logger.LogInformation("Finding records for sui {sui}", sui);
        var recordPointers = await recordFinder.FindRecordsAsync(sui, cancellationToken);
        logger.LogInformation(
            "Found {numOfRecordsPointers} records for sui {sui}",
            recordPointers.Length,
            sui
        );

        // Fetch records
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation("Fetching records for sui {sui}", sui);
        var unconsolidatedData = await recordFetcher.FetchRecordsAsync(
            sui,
            recordPointers,
            cancellationToken
        );
        logger.LogInformation(
            "Fetched {numOfRecordsFetched} records for sui {sui} ({numOfRecordsFailedFetching} records failed to fetch)",
            unconsolidatedData.CountOfRecordsSuccessfullyFetched,
            sui,
            unconsolidatedData.FailedFetches.Length
        );

        // Consolidate records
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation("Consolidating records for sui {sui}", sui);
        var consolidatedData = recordConsolidator.ConsolidateRecords(unconsolidatedData);
        logger.LogInformation("Consolidated records for sui {sui}", sui);

        // Apply aggregations
        var aggregatedConsolidatedData = consolidatedDataAggregator.ApplyAggregations(
            consolidatedData
        );

        return aggregatedConsolidatedData;
    }
}
