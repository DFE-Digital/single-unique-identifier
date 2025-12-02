namespace SUI.Custodians.Domain.Models;

public record LinkedCrimeRiskV1
{
    /// <summary>
    /// Crime - Risk - Date
    /// </summary>
    /// <example>2025-10-22</example>
    public DateOnly? Date { get; init; }

    /// <summary>
    /// Crime - Risk - Type
    /// </summary>
    /// <example>CriminalExploitation</example>
    public CrimeRiskTypeV1? RiskType { get; init; }
}
