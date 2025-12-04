using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public class TransferJob(
    IRecordFinder recordFinder,
    IRecordFetcher recordFetcher,
    IRecordConsolidator recordConsolidator,
    IEducationAttendanceTransformer educationAttendanceTransformer,
    IHealthAttendanceAggregator healthAttendanceAggregator,
    IMissingEpisodesTransformer missingEpisodesTransformer,
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<TransferJob> logger,
    TimeProvider timeProvider
) : ITransferJob
{
    public async Task<ConformedData> TransferAsync(Guid jobId, string sui)
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

        // Apply conformations (transformations and aggregations)
        cancellationToken.ThrowIfCancellationRequested();
        return new ConformedData(jobId, consolidatedData, timeProvider.GetUtcNow())
        {
            EducationAttendanceSummaries = educationAttendanceTransformer.ApplyTransformation(
                consolidatedData
            ),
            HealthAttendanceSummaries = healthAttendanceAggregator.ApplyAggregation(
                consolidatedData
            ),
            ChildrensSocialCareReferralSummaries = null,
            CrimeMissingEpisodesSummaries = missingEpisodesTransformer.ApplyTransformation(
                consolidatedData
            ),
        };
    }
}
