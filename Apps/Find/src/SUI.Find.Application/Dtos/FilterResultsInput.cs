using SUI.Find.Application.Models;

namespace SUI.Find.Application.Dtos;

public sealed record FilterResultsInput(
    string SourceOrgId,
    string DestOrgId,
    string DestOrgType,
    IReadOnlyList<SearchResultItem> Items,
    DsaPolicyDefinition DsaPolicy,
    string Purpose
);
