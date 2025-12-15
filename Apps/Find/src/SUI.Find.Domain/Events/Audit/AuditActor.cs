namespace SUI.Find.Domain.Events.Audit;

public sealed class AuditActor
{
    public required string ActorId { get; set; } // e.g. clientId
    public required string ActorRole { get; set; } = "Organisation"; // e.g. Organisation
}
