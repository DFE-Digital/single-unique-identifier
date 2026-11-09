using SUI.NotificationService.Application.Models;

namespace SUI.NotificationService.Application.Interfaces;

public interface IWebhookBroadcaster
{
    /// <summary>
    /// Broadcasts the notification to all provided suppliers.
    /// Returns true if any delivery suffered a retryable failure.
    /// </summary>
    Task<bool> BroadcastAsync(
        PdsRecordChangeNotification notification,
        IReadOnlyList<SupplierWebhook> suppliers,
        CancellationToken cancellationToken
    );
}
