using SUI.Find.Application.Models;

namespace SUI.Find.Application.Dtos;

public record SearchOrchestratorInput(
    string Suid,
    SearchJobMetadata Metadata,
    PolicyContext PolicyContext
);
