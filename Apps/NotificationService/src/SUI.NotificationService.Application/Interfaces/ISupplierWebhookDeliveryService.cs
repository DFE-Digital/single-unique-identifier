using SUI.NotificationService.Application.Models;

namespace SUI.NotificationService.Application.Interfaces;

public interface ISupplierWebhookDeliveryService
{
    Task<WebhookDeliveryResult> DeliverAsync(
        WebhookDeliveryRequest request,
        CancellationToken cancellationToken = default
    );
}
