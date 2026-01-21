using Azure.Storage.Queues;

namespace SUI.Find.Infrastructure.Clients;

/// <summary>
/// Lightweight wrapper for SendMessageAsync
/// </summary>
public interface IAuditQueueSender
{
    Task SendMessageAsync(string message, CancellationToken cancellationToken);
}

/// <summary>
/// <inheritdoc/>
/// </summary>
/// <param name="queueClient"></param>
public class AzureQueueSender(QueueClient queueClient) : IAuditQueueSender
{
    public Task SendMessageAsync(string message, CancellationToken cancellationToken) =>
        queueClient.SendMessageAsync(message, cancellationToken);
}
