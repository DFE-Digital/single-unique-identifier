using SUI.StubCustodians.API.Client;

namespace SUI.Transfer.Domain;

public record AggregatedData(Guid JobId, ConsolidatedData ConsolidatedData)
{
    public string Sui { get; } = ConsolidatedData.Sui;

    public ConsolidatedData ConsolidatedData { get; } = ConsolidatedData;

    public DateTimeOffset CreatedDate { get; } = DateTimeOffset.Now;

    public required EducationAttendanceV1? EducationAttendanceCurrentAcademicYear { get; init; }

    public required EducationAttendanceV1? EducationAttendanceLastAcademicYear { get; init; }

    public required HealthAttendanceSummary? HealthAttendanceSummaryLast12Months { get; init; }

    public required HealthAttendanceSummary? HealthAttendanceSummaryLast5Years { get; init; }

    public required ChildrensSocialCareReferralSummary[]? CSCReferralSummaryPast6Months { get; init; }

    public required ChildrensSocialCareReferralSummary[]? CSCReferralSummaryPast12Months { get; init; }

    public required ChildrensSocialCareReferralSummary[]? CSCReferralSummaryPast5Years { get; init; }
}
