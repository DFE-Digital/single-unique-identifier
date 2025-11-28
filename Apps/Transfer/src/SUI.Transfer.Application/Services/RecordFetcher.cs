using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public class RecordFetcher : IRecordFetcher
{
    public Task<UnconsolidatedData> FetchRecordsAsync(
        string sui,
        RecordPointer[] recordPointers,
        CancellationToken cancellationToken
    )
    {
        return Task.FromResult(
            new UnconsolidatedData(sui)
            {
                ChildPersonalDetailsRecords = [],
                ChildSocialCareDetailsRecords = [],
                EducationDetailsRecords = [],
                ChildHealthDataRecords = [],
                ChildLinkedCrimeDataRecords = [],
                FailedFetches = [],
            }
        );
    }
}
