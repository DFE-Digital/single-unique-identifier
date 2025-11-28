using SUI.StubCustodians.API.Client;

namespace SUI.Transfer.Domain;

public record AggregatedData(ConsolidatedData ConsolidatedData)
{
    public required EducationAttendanceV1? EducationAttendanceCurrentAcademicYear { get; init; }

    public required EducationAttendanceV1? EducationAttendanceLastAcademicYear { get; init; }

    public required HealthAttendanceSummary? HealthAttendanceSummaryLast12Months { get; init; }

    public required HealthAttendanceSummary? HealthAttendanceSummaryLast5Years { get; init; }

    public required CSCReferralSummary[]? CSCReferralSummaryPast6Months { get; init; }

    public required CSCReferralSummary[]? CSCReferralSummaryPast12Months { get; init; }

    public required CSCReferralSummary[]? CSCReferralSummaryPast5Years { get; init; }
}
