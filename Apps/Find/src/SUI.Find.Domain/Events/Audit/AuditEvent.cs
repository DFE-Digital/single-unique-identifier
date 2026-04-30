using System.Text.Json;

namespace SUI.Find.Domain.Events.Audit;

public class AuditEvent
{
    public required string EventId { get; set; }
    public required string EventName { get; set; } // e.g. "PEP_INTERACTION", "HTTP_REQUEST", etc.
    public required string ServiceName { get; set; } // e.g. PEP-Service
    public required AuditActor Actor { get; set; }
    public required JsonElement Payload { get; set; }
    public required DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public required string CorrelationId { get; set; }
    public string? TraceParent { get; set; }
}
