namespace SUI.Find.Domain.Models;

public record AuditAccessMessage
{
    public required string EventType { get; init; }
    public required string ClientId { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string Method { get; init; }
    public required string Path { get; init; }
    public required string CorrelationId { get; init; }
    public string Suid { get; init; } = string.Empty;
}
