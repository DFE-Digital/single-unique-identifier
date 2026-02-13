using UIHarness.Models;

namespace UIHarness.Interfaces;

public interface IFetchARecord
{
    Task<FetchRecordResponse> FetchAsync(Custodian custodian, string nhsNumber, string recordType, CancellationToken cancellationToken);
}