using UIHarness.Models;

namespace UIHarness.Interfaces;

public interface IPersonRepository
{
    Task<IReadOnlyList<PersonRecord>> GetAllAsync(CancellationToken cancellationToken);
}
