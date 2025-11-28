using SUI.Find.Application.Models;

namespace SUI.Find.Application.Dtos;

public record SearchJobOrchestrationInput(
    string Suid,
    SearchJobMetadata Metadata,
    PolicyContext PolicyContext
);
