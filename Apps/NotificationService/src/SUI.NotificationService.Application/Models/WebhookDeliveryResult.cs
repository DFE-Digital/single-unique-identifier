namespace SUI.NotificationService.Application.Models;

public record WebhookDeliveryResult(
    DeliveryOutcome Outcome,
    bool ResponseReceived,
    int? HttpStatusCode,
    bool TimedOut,
    bool ConnectionFailed,
    TimeSpan Duration
);

public enum DeliveryOutcome
{
    Success,
    RetryableFailure,
    PermanentFailure,
    SkippedPreviouslyAcknowledged,
}
