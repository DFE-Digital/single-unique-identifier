using SUI.Transfer.Application.Services;
using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Unit.Tests.Services;

public class AggregatedDataRepositoryTests
{
    [Fact]
    public async Task AddOrUpdateAsync_Test()
    {
        var sut = new AggregatedDataRepository();

        var action = async () =>
            await sut.AddOrUpdateAsync(
                new AggregatedData(
                    JobId: Guid.NewGuid(),
                    new ConsolidatedData("9990000123")
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
                    CSCReferralSummaryPast6Months = [],
                    CSCReferralSummaryPast12Months = [],
                    CSCReferralSummaryPast5Years = [],
                }
            );

        // ACT & ASSERT
        await action.Should().NotThrowAsync();
    }
}
