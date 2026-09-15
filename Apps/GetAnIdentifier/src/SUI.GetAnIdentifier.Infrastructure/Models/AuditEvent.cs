using System.Net;

namespace SUI.GetAnIdentifier.Infrastructure.Models;

public class AuditEvent
{
    public required string EventName { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
    public required string CorrelationId { get; set; }
    public required string TraceParent { get; set; }
    public string? Method { get; set; }
    public string? Url { get; set; }
    public HttpStatusCode? StatusCode { get; set; }
    public string? CallerId { get; set; }
}
