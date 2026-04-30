using SUI.UIHarness.Web.Models;

namespace SUI.UIHarness.Web.Services;

public interface IFindApiAuthClientProvider
{
    IReadOnlyList<AuthClient> GetAuthClients();
}
