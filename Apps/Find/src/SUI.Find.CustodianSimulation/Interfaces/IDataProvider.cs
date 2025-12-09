using SUI.Find.CustodianSimulation.Models;

namespace SUI.Find.CustodianSimulation.Interfaces;

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
