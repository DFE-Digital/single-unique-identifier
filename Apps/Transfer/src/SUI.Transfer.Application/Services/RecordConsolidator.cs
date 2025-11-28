using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public class RecordConsolidator : IRecordConsolidator
{
    public ConsolidatedData ConsolidateRecords(UnconsolidatedData unconsolidatedData)
    {
        return new ConsolidatedData(unconsolidatedData.Sui)
        {
            ChildPersonalDetailsRecord = null,
            ChildSocialCareDetailsRecord = null,
            EducationDetailsRecord = null,
            ChildHealthDataRecord = null,
            ChildLinkedCrimeDataRecord = null,
            CountOfRecordsSuccessfullyFetched =
                unconsolidatedData.CountOfRecordsSuccessfullyFetched,
            FailedFetches = [],
        };
    }
}
