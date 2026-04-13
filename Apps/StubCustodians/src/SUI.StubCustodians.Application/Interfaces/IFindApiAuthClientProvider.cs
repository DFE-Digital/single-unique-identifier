using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Interfaces;

public interface IFindApiAuthClientProvider
{
    IReadOnlyList<AuthClient> GetAuthClients();
}
