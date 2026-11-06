namespace SUI.NotificationService.Application.Models;

public record WebhookDeliveryReceipt(
    string SourceEventId,
    string SupplierId,
    Guid DeliveryId,
    string ContractVersion,
    DateTimeOffset AcknowledgedAt,
    int HttpStatusCode
);
