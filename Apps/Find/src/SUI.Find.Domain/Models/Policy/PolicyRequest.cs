namespace SUI.Find.Domain.Models.Policy;

public record PolicyCheckRequest(
    string SourceOrgId,
    string DestOrgId,
    string DataType,
    string Purpose,
    string Mode
);