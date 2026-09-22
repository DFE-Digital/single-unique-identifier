namespace SUI.NotificationService.Application.Models;

public record WebhookDeliveryRequest(
    Guid SourceEventId,
    LifecycleEventType EventType,
    DateTimeOffset OccurredAt,
    string AffectedNhsNumber,
    Guid DeliveryId,
    int DeliveryAttempt,
    Guid CorrelationId,
    string EndpointUrl,
    string KeyId,
    string SecretKeyVaultReference
);
