using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Interfaces;

public interface IManifestService
{
    Task<IList<SearchResultItem>> GetManifestForOrganisation(
        string orgId,
        string personId,
        string baseUrl,
        string? recordType,
        CancellationToken cancellationToken
    );
}
