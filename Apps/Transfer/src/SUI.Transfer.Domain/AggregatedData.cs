using SUI.StubCustodians.API.Client;

namespace SUI.Transfer.Domain;

public record AggregatedData(Guid JobId, ConsolidatedData ConsolidatedData)
{
    public string Sui { get; } = ConsolidatedData.Sui;

    public ConsolidatedData ConsolidatedData { get; } = ConsolidatedData;

    public DateTimeOffset CreatedDate { get; } = DateTimeOffset.Now;

    public required EducationAttendanceSummaries? EducationAttendanceSummaries { get; init; }

    public required HealthAttendanceSummaries? HealthAttendanceSummaries { get; init; }

    public required ChildrensSocialCareReferralSummaries? ChildrensSocialCareReferralSummaries { get; init; }

    public required CrimeMissingEpisodeV1[]? CrimeMissingEpisodesPast6Months { get; set; }
}
