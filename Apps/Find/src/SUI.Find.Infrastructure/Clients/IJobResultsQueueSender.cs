namespace SUI.Find.Infrastructure.Clients;

/// <summary>
/// Lightweight wrapper for SendMessageAsync
/// </summary>
public interface IJobResultsQueueSender
{
    Task SendMessageAsync(string message, CancellationToken cancellationToken);
}
