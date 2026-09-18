namespace SUI.NotificationService.Application.Models;

public record WebhookDeliveryResult(
    bool ResponseReceived,
    int? HttpStatusCode,
    bool TimedOut,
    bool ConnectionFailed,
    TimeSpan Duration
);
