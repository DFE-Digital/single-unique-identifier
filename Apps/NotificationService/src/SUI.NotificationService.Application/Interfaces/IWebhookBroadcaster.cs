using SUI.NotificationService.Application.Models;

namespace SUI.NotificationService.Application.Interfaces;

public interface IWebhookBroadcaster
{
    /// <summary>
    /// Broadcasts all notifications to the enabled suppliers.
    /// Returns an aggregate result for each notification detailing the delivery outcomes.
    /// </summary>
    Task<IReadOnlyList<BroadcastAggregateResult>> BroadcastBatchAsync(
        IReadOnlyList<PdsRecordChangeNotification> notifications,
        CancellationToken cancellationToken
    );
}
