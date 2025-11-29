using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public class ConsolidatedDataAggregator : IConsolidatedDataAggregator
{
    public AggregatedData ApplyAggregations(Guid jobId, ConsolidatedData consolidatedData)
    {
        return new AggregatedData(jobId, consolidatedData)
        {
            EducationAttendanceCurrentAcademicYear = null,
            EducationAttendanceLastAcademicYear = null,
            HealthAttendanceSummaryLast12Months = null,
            HealthAttendanceSummaryLast5Years = null,
            CSCReferralSummaryPast6Months = null,
            CSCReferralSummaryPast12Months = null,
            CSCReferralSummaryPast5Years = null,
        };
    }
}
