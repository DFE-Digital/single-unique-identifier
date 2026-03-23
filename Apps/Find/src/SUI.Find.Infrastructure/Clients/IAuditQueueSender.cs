namespace SUI.Find.Infrastructure.Clients;

/// <summary>
/// Lightweight wrapper for SendMessageAsync
/// </summary>
public interface IAuditQueueSender
{
    Task SendMessageAsync(string message, CancellationToken cancellationToken);
}
