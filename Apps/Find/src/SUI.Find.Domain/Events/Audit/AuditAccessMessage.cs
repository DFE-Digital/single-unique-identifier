namespace SUI.Find.Domain.Events.Audit;

public record AuditAccessMessage
{
    public required string Method { get; init; }
    public required string Path { get; init; }
    public string Suid { get; init; } = string.Empty;
}
