using System.Text.Json.Serialization;

namespace SUI.NotificationService.Application.Models;

public record LifecycleNotification(
    [property: JsonPropertyName("eventId"), JsonPropertyOrder(2)] Guid EventId,
    [property: JsonPropertyName("eventType"), JsonPropertyOrder(3)] LifecycleEventType EventType,
    [property: JsonPropertyName("occurredAt"), JsonPropertyOrder(4)] DateTime OccurredAt,
    [property: JsonPropertyName("affectedNhsNumber"), JsonPropertyOrder(5)] string AffectedNhsNumber
)
{
    // The contract specifies this must be a constant value of "1"
    [JsonPropertyName("schemaVersion"), JsonPropertyOrder(1)]
    public string SchemaVersion { get; } = "1";
}
