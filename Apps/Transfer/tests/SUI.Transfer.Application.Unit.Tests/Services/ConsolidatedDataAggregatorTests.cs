using SUI.Transfer.Application.Services;
using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Unit.Tests.Services;

public class ConsolidatedDataAggregatorTests
{
    [Fact]
    public void ApplyAggregations_Does_Aggregate_AsExpected()
    {
        var sut = new ConsolidatedDataAggregator();
        var jobId = Guid.NewGuid();

        // ACT
        var result = sut.ApplyAggregations(
            jobId,
            new ConsolidatedData("XXX 000 1234")
            {
                ChildPersonalDetailsRecord = null,
                ChildSocialCareDetailsRecord = null,
                EducationDetailsRecord = null,
                ChildHealthDataRecord = null,
                ChildLinkedCrimeDataRecord = null,
                CountOfRecordsSuccessfullyFetched = 0,
                FailedFetches = [],
            }
        );

        // ASSERT
        result
            .Should()
            .BeEquivalentTo(
                new AggregatedData(
                    jobId,
                    new ConsolidatedData("XXX 000 1234")
                    {
                        ChildPersonalDetailsRecord = null,
                        ChildSocialCareDetailsRecord = null,
                        EducationDetailsRecord = null,
                        ChildHealthDataRecord = null,
                        ChildLinkedCrimeDataRecord = null,
                        CountOfRecordsSuccessfullyFetched = 0,
                        FailedFetches = [],
                    }
                )
                {
                    EducationAttendanceCurrentAcademicYear = null,
                    EducationAttendanceLastAcademicYear = null,
                    HealthAttendanceSummaryLast12Months = null,
                    HealthAttendanceSummaryLast5Years = null,
                    CSCReferralSummaryPast6Months = null,
                    CSCReferralSummaryPast12Months = null,
                    CSCReferralSummaryPast5Years = null,
                },
                options => options.Excluding(x => x.CreatedDate)
            );
    }
}
