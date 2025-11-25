namespace Interfaces;

public interface IDataProvider
{
    Task<IReadOnlyList<CustodianRecord>> GetRecordsAsync(string orgId, string personId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CustodianRecord>> GetRecordsAsync(string orgId, string recordType, string personId, CancellationToken cancellationToken);

}
