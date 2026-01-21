using System.Net;
using SUI.Custodians.Domain.Models;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Services;

public class ManifestService : IManifestService
{
    public IList<SearchResultItem> GetManifestForOrganisation(string orgId, string personId)
    {
        return new List<SearchResultItem>
        {
            new("Test", "test", nameof(ChildrensServicesDetailsRecord), "https://localhost:5323/"),
        };
    }
}

public interface IManifestService
{
    IList<SearchResultItem> GetManifestForOrganisation(string orgId, string personId);
}
