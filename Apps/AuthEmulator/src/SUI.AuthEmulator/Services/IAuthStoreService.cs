using SUI.AuthEmulator.Models;

namespace SUI.AuthEmulator.Services;

public interface IAuthStoreService
{
    Task<AuthStore> GetAuthStoreAsync();
    Task<Result<AuthClient>> GetClientByCredentials(string clientId, string clientSecret);
}
