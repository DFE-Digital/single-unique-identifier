using SUI.Find.Application.Enums;

namespace SUI.Find.Application.Models;

public sealed record PolicyDecisionRequest(
    string SourceOrgId,
    string DestinationOrgId,
    string? RecordType,
    ShareMode Mode,
    string Purpose
);
