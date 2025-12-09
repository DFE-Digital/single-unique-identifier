namespace SUI.Custodians.Domain.Models;

/// <summary>
/// Crime-linked data about a specific child.
/// </summary>
public record CrimeDataRecordV1 : SuiRecord
{
    /// <summary>
    /// Police marker details
    /// </summary>
    /// <example>Individuals at the address may resort to violent behaviour</example>
    public string? PoliceMarkerDetails { get; init; }

    /// <summary>
    /// Crime - Services known to
    /// </summary>
    /// <example>Youth justice service (YJS), Police</example>
    public IReadOnlyCollection<string>? ServicesKnownTo { get; init; }

    /// <summary>
    /// Last Police Protection Power event
    /// </summary>
    public string? LastPoliceProtectionPowerEvent { get; init; }

    /// <summary>
    /// Police - Missing Episodes
    /// </summary>
    public IReadOnlyCollection<CrimeMissingEpisodeV1>? MissingEpisodes { get; init; }

    /// <summary>
    /// Linked crime risks
    /// </summary>
    public IReadOnlyCollection<LinkedCrimeRiskV1>? LinkedCrimeRisks { get; init; }
}
