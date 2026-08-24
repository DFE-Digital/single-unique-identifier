namespace SUI.GetAnIdentifier.Application.Models;

public sealed record AuditLogEntry
{
    public required string EventType { get; init; }
    public required string CallerId { get; init; }
    public required string CorrelationId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public string? HttpMethod { get; init; }
    public string? RequestPath { get; init; }
    public object? RequestBody { get; init; }
    public int? StatusCode { get; init; }
    public string? ResponseSummary { get; init; }
}
