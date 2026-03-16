using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Queues;

namespace SUI.Find.Infrastructure.Clients;

/// <summary>
/// Lightweight wrapper for SendMessageAsync
/// </summary>
public interface ISearchJobQueueSender
{
    Task SendMessageAsync(string message, CancellationToken cancellationToken);
}

/// <summary>
/// <inheritdoc/>
/// </summary>
/// <param name="searchJobQueueClient"></param>
[ExcludeFromCodeCoverage(Justification = "Simple wrapper class")]
public class AzureSearchJobQueueSender(QueueClient searchJobQueueClient) : ISearchJobQueueSender
{
    public Task SendMessageAsync(string message, CancellationToken cancellationToken) =>
        searchJobQueueClient.SendMessageAsync(message, cancellationToken);
}
