using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SUI.SingleView.Application.Exceptions;
using SUI.SingleView.Application.Models;
using SUI.Transfer.API.Client;

namespace SUI.SingleView.Application.Services;

public class RecordService(
    ITransferApi transferApi,
    IPersonMapper personMapper,
    IOptions<HttpPollingOptions> httpPollingOptions,
    TimeProvider timeProvider,
    ILogger<RecordService> logger
) : IRecordService
{
    public async Task<PersonModel> GetRecordAsync(
        string nhsNumber,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var conformedData = await TransferAsync(nhsNumber, cancellationToken);

            return personMapper.Map(nhsNumber, conformedData);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "An error occurred when trying to get the record for {Id}",
                nhsNumber
            );
            throw new RecordException(
                $"An error occurred when trying to get the record for {nhsNumber}",
                ex
            );
        }
    }

    private async Task<ConformedData> TransferAsync(
        string nhsNumber,
        CancellationToken cancellationToken
    )
    {
        var jobId = (await transferApi.TransferPOSTAsync(nhsNumber, cancellationToken)).JobId;

        var startTime = timeProvider.GetTimestamp();
        while (timeProvider.GetElapsedTime(startTime) < httpPollingOptions.Value.PollTimeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var jobState = await transferApi.TransferGETAsync(jobId, cancellationToken);

            if (jobState.Status is TransferJobStatus.Completed)
            {
                return (await transferApi.ResultsAsync(jobId, cancellationToken)).Data;
            }

            if (jobState.Status is not (TransferJobStatus.Queued or TransferJobStatus.Running))
            {
                throw new Exception(
                    $"Transfer job {jobId} did not complete as expected. Status was: {jobState.Status}"
                );
            }

            await Task.Delay(
                httpPollingOptions.Value.PollInterval,
                timeProvider,
                cancellationToken
            );
        }

        throw new Exception(
            $"Transfer job {jobId} did not finish within allotted time: {httpPollingOptions.Value.PollTimeout}"
        );
    }
}
