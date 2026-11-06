using SUI.UIHarness.Web.Models;

namespace SUI.UIHarness.Web.Services;

public interface IGetAnIdentifierAuthClientProvider
{
    IReadOnlyList<AuthClient> GetAuthClients();
}
