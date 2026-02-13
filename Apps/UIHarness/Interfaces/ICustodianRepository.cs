using UIHarness.Models;

namespace UIHarness.Interfaces;

public interface ICustodianRepository
{
    Task<IReadOnlyList<Custodian>> GetAllAsync(CancellationToken cancellationToken);
    Task<Custodian?> GetByIdAsync(string custodianId, CancellationToken cancellationToken);
}