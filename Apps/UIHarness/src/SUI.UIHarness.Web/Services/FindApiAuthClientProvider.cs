using System.Text.Json;
using SUI.UIHarness.Web.Models;

namespace SUI.UIHarness.Web.Services;

public class FindApiAuthClientProvider : IFindApiAuthClientProvider
{
    private readonly Lazy<IReadOnlyList<AuthClient>> _authClients = new(Load);

    public IReadOnlyList<AuthClient> GetAuthClients() => _authClients.Value;

    private static AuthClient[] Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "auth-clients-inbound.json");

        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"auth-clients-inbound.json not found: {path}");
        }

        var json = File.ReadAllText(path);

        var data =
            JsonSerializer.Deserialize<AuthStore>(json, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException("Invalid auth-clients-inbound.json");

        return data.Clients ?? [];
    }

    private record AuthStore(AuthClient[]? Clients);
}
