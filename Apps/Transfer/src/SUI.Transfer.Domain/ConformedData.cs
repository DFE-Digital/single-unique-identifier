using System.Text.Json.Serialization;
using SUI.Custodians.API.Client;

namespace SUI.Transfer.Domain;

/// <summary>
/// Represents consolidated and conformed (transformed and aggregated) data about a person with a specified single-unique-identifier.
/// </summary>
public record ConformedData(Guid JobId, ConsolidatedData ConsolidatedData)
{
    [JsonIgnore]
    public string Sui { get; } = ConsolidatedData.Sui;

    public ConsolidatedData ConsolidatedData { get; } = ConsolidatedData;

    public DateTimeOffset CreatedDate { get; } = DateTimeOffset.Now;

    public required EducationAttendanceSummaries? EducationAttendanceSummaries { get; init; }

    public required HealthAttendanceSummaries? HealthAttendanceSummaries { get; init; }

    public required ChildrensSocialCareReferralSummaries? ChildrensSocialCareReferralSummaries { get; init; }

    public required CrimeMissingEpisodeV1[]? CrimeMissingEpisodesPast6Months { get; set; }

    [JsonIgnore]
    public Guid JobId { get; init; } = JobId;
}
