using SUI.NotificationService.Application.Models;

namespace SUI.NotificationService.Application.Interfaces;

public interface IWebhookReceiptRepository
{
    Task AddAsync(WebhookDeliveryReceipt receipt, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(
        string sourceEventId,
        string supplierId,
        CancellationToken cancellationToken = default
    );
}
