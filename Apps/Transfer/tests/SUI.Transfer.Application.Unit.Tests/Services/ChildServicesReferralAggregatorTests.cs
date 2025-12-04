using SUI.Transfer.Application.Services;
using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Unit.Tests.Services;

public class ChildServicesReferralAggregatorTests
{
    [Fact]
    public void ApplyAggregation_Test()
    {
        var sut = new ChildServicesReferralAggregator();

        // ACT
        var result = sut.ApplyAggregation(
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
        result.Should().BeNull();
    }
}
