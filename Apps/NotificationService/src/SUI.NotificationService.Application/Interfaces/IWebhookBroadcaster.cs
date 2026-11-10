using SUI.NotificationService.Application.Models;

namespace SUI.NotificationService.Application.Interfaces;

public interface IWebhookBroadcaster
{
    /// <summary>
    /// Broadcasts all notifications to the provided suppliers.
    /// Returns a list of MESH message IDs that were successfully processed and can be acknowledged.
    /// </summary>
    Task<IReadOnlyList<string>> BroadcastBatchAsync(
        IReadOnlyList<PdsRecordChangeNotification> notifications,
        IReadOnlyList<SupplierWebhook> suppliers,
        CancellationToken cancellationToken
    );
}
