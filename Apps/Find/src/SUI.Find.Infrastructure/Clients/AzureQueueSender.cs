using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Queues;

namespace SUI.Find.Infrastructure.Clients;

/// <summary>
/// Sends messages to an Azure Storage Queue for processing.
/// </summary>
/// <param name="queueClient"></param>
[ExcludeFromCodeCoverage(Justification = "Simple wrapper class")]
public class AzureQueueSender(QueueClient queueClient) : IAuditQueueSender, IJobResultsQueueSender
{
    public Task SendMessageAsync(string message, CancellationToken cancellationToken) =>
        queueClient.SendMessageAsync(message, cancellationToken);
}
