using SUI.Transfer.Domain;
using SUI.Transfer.Domain.Services;

namespace SUI.Transfer.Application.Services;

public class RecordConsolidator(
    IConsolidateRecordCollectionsService consolidateRecordCollectionsService,
    IConsolidationFieldRanker consolidationFieldRanker
) : IRecordConsolidator
{
    public ConsolidatedData ConsolidateRecords(UnconsolidatedData unconsolidatedData) =>
        new(unconsolidatedData.Sui)
        {
            PersonalDetailsRecord = consolidateRecordCollectionsService.ConsolidateRecords(
                unconsolidatedData.PersonalDetailsRecords,
                consolidationFieldRanker.RankField
            ),

            ChildrensServicesDetailsRecord = consolidateRecordCollectionsService.ConsolidateRecords(
                unconsolidatedData.ChildrensServicesDetailsRecords,
                consolidationFieldRanker.RankField
            ),

            EducationDetailsRecord = consolidateRecordCollectionsService.ConsolidateRecords(
                unconsolidatedData.EducationDetailsRecords,
                consolidationFieldRanker.RankField
            ),

            HealthDataRecord = consolidateRecordCollectionsService.ConsolidateRecords(
                unconsolidatedData.HealthDataRecords,
                consolidationFieldRanker.RankField
            ),

            CrimeDataRecord = consolidateRecordCollectionsService.ConsolidateRecords(
                unconsolidatedData.CrimeDataRecords,
                consolidationFieldRanker.RankField
            ),

            CountOfRecordsSuccessfullyFetched =
                unconsolidatedData.CountOfRecordsSuccessfullyFetched,

            FailedFetches = unconsolidatedData.FailedFetches,
        };
}
