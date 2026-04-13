namespace SUI.UIHarness.Web.Models.Records;

public record LinkedCrimeRisk
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
    public CrimeRiskType? RiskType { get; init; }
}
