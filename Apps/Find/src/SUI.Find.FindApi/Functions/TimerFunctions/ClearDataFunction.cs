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
    private const string Schedule = "0 0 4 * * *"; // Once per day, at 4am
    private const int BatchSize = 99;

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
            var tableEntities = tableClient.QueryAsync<FetchUrlMappingEntity>(
                e => e.Timestamp < purgeCutoff,
                maxPerPage: BatchSize,
                select: ["PartitionKey", "RowKey"],
                cancellationToken: cancellationToken
            );

            var totalEntitiesRemoved = 0;

            await foreach (var tableEntityPage in tableEntities.AsPages())
            {
                foreach (
                    var partitionGrouping in tableEntityPage.Values.GroupBy(x => x.PartitionKey)
                )
                {
                    var transactionActions = partitionGrouping
                        .Select(entity => new TableTransactionAction(
                            TableTransactionActionType.Delete,
                            entity
                        ))
                        .ToList();

                    var transactionResponse = await tableClient.SubmitTransactionAsync(
                        transactionActions,
                        cancellationToken
                    );

                    logger.LogInformation(
                        "Batch of {Count} table entities successfully removed from table. Partition key: {PartitionKey}",
                        transactionResponse.Value.Count(x => !x.IsError),
                        partitionGrouping.Key
                    );

                    totalEntitiesRemoved += transactionResponse.Value.Count;
                }
            }

            if (totalEntitiesRemoved == 0)
            {
                logger.LogInformation("No table entities to delete.");
            }
            else
            {
                logger.LogInformation(
                    "{Count} total table entities successfully deleted.",
                    totalEntitiesRemoved
                );
            }
        }
        catch (TableTransactionFailedException ex)
        {
            logger.LogError(
                ex,
                "One of the batch delete transactions failed. {Message}",
                ex.Message
            );
            throw;
        }
        catch (RequestFailedException ex)
        {
            logger.LogError(ex, "Error querying or clearing table. {Message}", ex.Message);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "This transaction batch has already been submitted");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to clear table");
            throw;
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
                "{Count} entities deleted from instance history",
                purgeHistoryResult.PurgedInstanceCount
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to clear instance history");
            throw;
        }
    }
}
