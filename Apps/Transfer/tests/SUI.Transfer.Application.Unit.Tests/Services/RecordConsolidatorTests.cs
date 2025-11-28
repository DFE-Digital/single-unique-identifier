using SUI.Transfer.Application.Services;
using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Unit.Tests.Services;

public class RecordConsolidatorTests
{
    [Fact]
    public void ConsolidateRecords_Does_Consolidate_AsExpected()
    {
        var sut = new RecordConsolidator();

        // ACT
        var result = sut.ConsolidateRecords(
            new UnconsolidatedData("XXX 000 1234")
            {
                ChildPersonalDetailsRecords = [],
                ChildSocialCareDetailsRecords = [],
                EducationDetailsRecords = [],
                ChildHealthDataRecords = [],
                ChildLinkedCrimeDataRecords = [],
                FailedFetches = [],
            }
        );

        // ASSERT
        Assert.Equal(
            new ConsolidatedData("XXX 000 1234")
            {
                ChildPersonalDetailsRecord = null,
                ChildSocialCareDetailsRecord = null,
                EducationDetailsRecord = null,
                ChildHealthDataRecord = null,
                ChildLinkedCrimeDataRecord = null,
                CountOfRecordsSuccessfullyFetched = 0,
                FailedFetches = [],
            },
            result
        );
    }
}
