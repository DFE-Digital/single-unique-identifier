using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Interfaces;

public interface IDataProvider
{
    Task<IReadOnlyList<CustodianRecord>> GetRecordsAsync(
        string orgId,
        string personId,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<CustodianRecord>> GetRecordsAsync(
        string orgId,
        string recordType,
        string personId,
        CancellationToken cancellationToken
    );

    Task<CustodianRecord?> GetRecordByIdAsync(
        string orgId,
        string recordId,
        CancellationToken cancellationToken
    );
}
