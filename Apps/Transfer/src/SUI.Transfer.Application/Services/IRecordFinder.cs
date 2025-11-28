using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public interface IRecordFinder
{
    Task<RecordPointer[]> FindRecordsAsync(string sui, CancellationToken cancellationToken);
}
