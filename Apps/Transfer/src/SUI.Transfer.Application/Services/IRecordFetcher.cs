using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public interface IRecordFetcher
{
    Task<UnconsolidatedData> FetchRecordsAsync(
        string sui,
        RecordPointer[] recordPointers,
        CancellationToken cancellationToken
    );
}
