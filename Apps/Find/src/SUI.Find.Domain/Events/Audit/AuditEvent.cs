namespace SUI.Find.Domain.Events.Audit;

public class AuditEvent<T>
{
    public required string EventId { get; set; } = Guid.NewGuid().ToString();

    public required DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public required string CorrelationId { get; set; }

    public required string ServiceName { get; set; } // e.g. PEP-Service

    public required string EventName { get; set; } // e.g. "PEP_INTERACTION", "HTTP_REQUEST", etc.

    public required AuditActor Actor { get; set; }

    public required T Payload { get; set; }
}
