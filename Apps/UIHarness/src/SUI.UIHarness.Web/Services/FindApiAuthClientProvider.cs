using System.Text.Json;
using SUI.UIHarness.Web.Models;

namespace SUI.UIHarness.Web.Services;

public class FindApiAuthClientProvider : IFindApiAuthClientProvider
{
    private readonly Lazy<IReadOnlyList<AuthClient>> _authClients;
    private readonly IConfiguration _configuration;

    public FindApiAuthClientProvider(IConfiguration configuration)
    {
        _configuration = configuration;
        _authClients = new Lazy<IReadOnlyList<AuthClient>>(Load);
    }

    public IReadOnlyList<AuthClient> GetAuthClients() => _authClients.Value;

    private AuthClient[] Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "auth-clients-inbound.json");

        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"auth-clients-inbound.json not found: {path}");
        }

        var json = File.ReadAllText(path);

        var store =
            JsonSerializer.Deserialize<AuthStore>(json, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException("Invalid auth-clients-inbound.json");

        return (store.Clients ?? [])
            .Select(client =>
                client with
                {
                    ClientId =
                        _configuration[$"AuthClientCredentials:{client.ClientId}:NewClientId"]
                        ?? client.ClientId,
                    ClientSecret =
                        _configuration[$"AuthClientCredentials:{client.ClientId}:NewClientSecret"]
                        ?? client.ClientSecret,
                }
            )
            .ToArray();
    }

    private record AuthStore(AuthClient[]? Clients);
}
