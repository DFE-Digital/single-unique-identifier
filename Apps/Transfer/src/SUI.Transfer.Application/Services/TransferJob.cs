using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public class TransferJob(
    IRecordFinder recordFinder,
    IRecordFetcher recordFetcher,
    IRecordConsolidator recordConsolidator,
    IEducationAttendanceAggregator educationAttendanceAggregator,
    IAggregatedDataRepository aggregatedDataRepository,
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
        var consolidatedData = recordConsolidator.ConsolidateRecords(unconsolidatedData);

        // Apply aggregations
        cancellationToken.ThrowIfCancellationRequested();
        var aggregatedData = new AggregatedData(jobId, consolidatedData)
        {
            EducationAttendanceSummaries = educationAttendanceAggregator.ApplyAggregation(
                consolidatedData
            ),
            HealthAttendanceSummaries = null,
            ChildrensSocialCareReferralSummaries = null,
            CrimeMissingEpisodesPast6Months = null,
        };

        // Store the Aggregated Data, with a configured Time to Live (TTL)
        await aggregatedDataRepository.AddOrUpdateAsync(aggregatedData);

        return aggregatedData;
    }
}
