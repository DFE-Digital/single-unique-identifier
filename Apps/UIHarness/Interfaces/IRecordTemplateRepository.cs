using UIHarness.Models;

namespace UIHarness.Interfaces;

public interface IRecordTemplateRepository
{
    Task<RecordTemplate?> GetByRecordTypeAsync(string recordType, CancellationToken cancellationToken);
}