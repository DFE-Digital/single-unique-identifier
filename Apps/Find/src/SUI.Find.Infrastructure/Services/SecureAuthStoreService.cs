using System.Diagnostics.CodeAnalysis;
using SUI.Find.Domain.Models;
using SUI.Find.Infrastructure.Models;

namespace SUI.Find.Infrastructure.Services;

[ExcludeFromCodeCoverage(Justification = "Not used yet")]
public class SecureAuthStoreService : IAuthStoreService
{
    public Task<AuthStore> GetAuthStoreAsync()
    {
        throw new NotImplementedException();
    }

    public Task<Result<AuthClient>> GetClientByCredentials(string clientId, string clientSecret)
    {
        throw new NotImplementedException();
    }
}
