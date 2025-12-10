using System.Text.Json.Serialization;
using SUI.Custodians.API.Client;

namespace SUI.Transfer.Domain;

/// <summary>
/// Represents consolidated and conformed (transformed and aggregated) data about a person with a specified single-unique-identifier.
/// </summary>
public record ConformedData(
    Guid JobId,
    ConsolidatedData ConsolidatedData,
    DateTimeOffset CreatedDate
)
{
    public string Sui { get; } = ConsolidatedData.Sui;

    public ConsolidatedData ConsolidatedData { get; } = ConsolidatedData;

    public required EducationAttendanceSummaries? EducationAttendanceSummaries { get; init; }

    public required HealthAttendanceSummaries? HealthAttendanceSummaries { get; init; }

    public required ChildServicesReferralSummaries? ChildServicesReferralSummaries { get; init; }

    public required CrimeMissingEpisodesSummaries? CrimeMissingEpisodesSummaries { get; init; }
}
