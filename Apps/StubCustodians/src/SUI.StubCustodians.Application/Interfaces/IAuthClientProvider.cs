using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Interfaces;

public interface IAuthClientProvider
{
    IReadOnlyList<AuthClient> GetAuthClients();
}
