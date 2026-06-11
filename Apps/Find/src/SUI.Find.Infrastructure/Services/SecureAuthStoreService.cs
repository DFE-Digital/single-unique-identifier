using System.Diagnostics.CodeAnalysis;
using SUI.Find.Infrastructure.Models;

namespace SUI.Find.Infrastructure.Services;

[ExcludeFromCodeCoverage(Justification = "Not used yet")]
public class SecureAuthStoreService : IAuthStoreService
{
    public Task<AuthStore> GetAuthStoreAsync()
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<string> GetScopesByClientId(string clientId)
    {
        throw new NotImplementedException();
    }

    public string GetOrganisationIdForClientId(string clientId)
    {
        throw new NotImplementedException();
    }
}
