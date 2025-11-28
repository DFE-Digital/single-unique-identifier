namespace SUI.Find.Application.Dtos;

public sealed record PolicyContext(string ClientId, IReadOnlyList<string> Scopes);

public record SearchJobMetadata(string PersonId, DateTime RequestedAtUtc);

public record SearchJobOrchestrationInput(
    string Suid,
    SearchJobMetadata Metadata,
    PolicyContext PolicyContext
);
