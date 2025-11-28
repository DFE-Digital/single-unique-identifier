using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public interface IRecordFinder
{
    Task<RecordPointer[]> FindRecordsAsync(string sui, CancellationToken cancellationToken);
}

public class RecordFinder : IRecordFinder
{
    public Task<RecordPointer[]> FindRecordsAsync(string sui, CancellationToken cancellationToken)
    {
        return Task.FromResult(
            new[]
            {
                new RecordPointer(
                    "LAC-SYSTEM-01",
                    "Example LA Case Management System",
                    "https://lac-system-01.example.gov.uk/api/records/ABC-123456"
                ),
                new RecordPointer(
                    "NHS-GP-01",
                    "Example GP Clinical System",
                    "https://nhs-gp-01.example.nhs.uk/patient/SUI-1234567890"
                ),
            }
        );
    }
}
