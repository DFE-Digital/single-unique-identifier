using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Interfaces;

public interface IOrgDirectoryProvider
{
    IReadOnlyList<Organisation> GetOrganisations();
}
