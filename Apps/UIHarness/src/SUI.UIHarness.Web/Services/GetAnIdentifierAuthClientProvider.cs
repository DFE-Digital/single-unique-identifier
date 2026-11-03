using System.Text.Json;
using SUI.UIHarness.Web.Models;

namespace SUI.UIHarness.Web.Services;

public class GetAnIdentifierAuthClientProvider : IGetAnIdentifierAuthClientProvider
{
    private readonly Lazy<IReadOnlyList<AuthClient>> _authClients;
    private readonly IConfiguration _configuration;

    public GetAnIdentifierAuthClientProvider(IConfiguration configuration)
    {
        _configuration = configuration;
        _authClients = new Lazy<IReadOnlyList<AuthClient>>(Load);
    }

    public IReadOnlyList<AuthClient> GetAuthClients() => _authClients.Value;

    private AuthClient[] Load()
    {
        var clients = new List<AuthClient>();
        var clientsFromSettings = _configuration.GetSection("AuthClientCredentials").GetChildren();
        foreach (var client in clientsFromSettings)
        {
            if (client.Value != null)
                clients.Add(new AuthClient(client.Key, client.Value, client.Key, true));
        }
        return clients.ToArray();
    }

    private record AuthStore(AuthClient[]? Clients);
}
