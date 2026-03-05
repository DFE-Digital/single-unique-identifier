using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SUI.Find.Infrastructure;
using SUI.Find.Infrastructure.Models;
using OrchestrationRuntimeStatus = Microsoft.DurableTask.Client.OrchestrationRuntimeStatus;

namespace SUI.Find.FindApi.Functions.TimerFunctions;

public class ClearDataFunction(
    ILogger<ClearDataFunction> logger,
    IConfiguration config,
    TimeProvider timeProvider,
    TableServiceClient tableServiceClient
)
{
    private const string Schedule = "0 0 4 * * *";

    [Function(nameof(ClearDataFunction))]
    [FixedDelayRetry(5, "00:00:10")]
    public async Task ClearData(
        [TimerTrigger(Schedule)] TimerInfo timerInfo,
        [DurableClient] DurableTaskClient taskClient,
        CancellationToken cancellationToken
    )
    {
        var tableClient = tableServiceClient.GetTableClient(
            InfrastructureConstants.StorageTableUrlMappings.TableName
        );

        var purgeCutoff = GetPurgeCutoff();

        await ClearMappingTable(tableClient, purgeCutoff, cancellationToken);
        await ClearInstanceHistory(purgeCutoff, taskClient, cancellationToken);
    }

    private DateTime GetPurgeCutoff()
    {
        var storageRetentionDaysString = config["StorageRetentionDays"];
        if (storageRetentionDaysString == null)
            throw new InvalidOperationException("Missing config value for StorageRetentionDays");

        var storageRetentionDays = int.Parse(storageRetentionDaysString);

        var purgeCutoff = timeProvider.GetUtcNow().AddDays(-storageRetentionDays);

        return purgeCutoff.DateTime;
    }

    private async Task ClearMappingTable(
        TableClient tableClient,
        DateTime purgeCutoff,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var transactionActions = new List<TableTransactionAction>();

            var tableEntities = tableClient.QueryAsync<FetchUrlMappingEntity>(
                e => e.Timestamp < purgeCutoff,
                select: ["PartitionKey", "RowKey"],
                cancellationToken: cancellationToken
            );

            await foreach (var tableEntity in tableEntities)
            {
                transactionActions.Add(
                    new TableTransactionAction(TableTransactionActionType.Delete, tableEntity)
                );
            }

            if (transactionActions.Count == 0)
            {
                logger.LogInformation("No table entities to delete.");
                return;
            }

            var transactionResponse = await tableClient.SubmitTransactionAsync(
                transactionActions,
                cancellationToken
            );

            foreach (var errorResponse in transactionResponse.Value.Where(x => x.IsError))
            {
                logger.LogError(
                    "Error submitting delete transaction. Request ID: {detail}. Reason: {error}",
                    errorResponse.ClientRequestId,
                    errorResponse.ReasonPhrase
                );
            }

            logger.LogInformation(
                "{count} entities successfully removed from table",
                transactionResponse.Value.Count(x => !x.IsError)
            );
        }
        catch (TableTransactionFailedException ex)
        {
            logger.LogError(
                ex,
                "One of the batch delete transactions failed. {message}",
                ex.Message
            );
        }
        catch (RequestFailedException ex)
        {
            logger.LogError(ex, "Error querying or clearing table. {message}", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "This transaction batch has already been submitted");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to clear table");
        }
    }

    private async Task ClearInstanceHistory(
        DateTime purgeCutoff,
        DurableTaskClient taskClient,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var filter = new PurgeInstancesFilter(
                DateTime.MinValue,
                purgeCutoff,
                [
                    OrchestrationRuntimeStatus.Completed,
                    OrchestrationRuntimeStatus.Failed,
                    OrchestrationRuntimeStatus.Terminated,
                ]
            );

            var purgeHistoryResult = await taskClient.PurgeAllInstancesAsync(
                filter,
                null,
                cancellationToken
            );

            logger.LogInformation(
                "{count} entities deleted from instance history",
                purgeHistoryResult.PurgedInstanceCount
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to clear instance history");
        }
    }
}
