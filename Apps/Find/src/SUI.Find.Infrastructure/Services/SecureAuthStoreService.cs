using SUI.Find.Infrastructure.Models;

namespace SUI.Find.Infrastructure.Services;

public class SecureAuthStoreService : IAuthStoreService
{
    public Task<Result<AuthClient>> GetClientByCredentials(string clientId, string clientSecret)
    {
        throw new NotImplementedException();
    }
}
