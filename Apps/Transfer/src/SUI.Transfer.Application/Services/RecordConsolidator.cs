using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public class RecordConsolidator : IRecordConsolidator
{
    public ConsolidatedData ConsolidateRecords(UnconsolidatedData unconsolidatedData)
    {
        return new ConsolidatedData(unconsolidatedData.Sui)
        {
            PersonalDetailsRecord = null,
            ChildrensServicesDetailsRecord = null,
            EducationDetailsRecord = null,
            HealthDataRecord = null,
            CrimeDataRecord = null,
            CountOfRecordsSuccessfullyFetched =
                unconsolidatedData.CountOfRecordsSuccessfullyFetched,
            FailedFetches = [],
        };
    }
}
