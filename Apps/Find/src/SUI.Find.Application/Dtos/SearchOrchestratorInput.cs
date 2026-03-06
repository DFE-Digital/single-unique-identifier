using SUI.Find.Application.Models;

namespace SUI.Find.Application.Dtos;

public record SearchOrchestratorInput(
    string Suid,
    SearchJobMetadata Metadata,
    PolicyContext PolicyContext
);

public record SearchProviderSubOrchestratorInput(
    SearchOrchestratorInput SearchInput,
    ProviderDefinition QueryProvider
);
