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
                PersonalDetailsRecords = [],
                ChildrensServicesDetailsRecords = [],
                EducationDetailsRecords = [],
                HealthDataRecords = [],
                CrimeDataRecords = [],
                FailedFetches = [],
            }
        );

        // ASSERT
        result
            .Should()
            .BeEquivalentTo(
                new ConsolidatedData("XXX 000 1234")
                {
                    PersonalDetailsRecord = null,
                    ChildrensServicesDetailsRecord = null,
                    EducationDetailsRecord = null,
                    HealthDataRecord = null,
                    CrimeDataRecord = null,
                    CountOfRecordsSuccessfullyFetched = 0,
                    FailedFetches = [],
                }
            );
    }
}
