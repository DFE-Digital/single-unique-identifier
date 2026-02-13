using UIHarness.Models;

namespace UIHarness.Interfaces;

public interface IFindARecord
{
    Task<IReadOnlyList<string>> FindRecordTypesAsync(Custodian custodian, string nhsNumber, CancellationToken cancellationToken);
}