using System.Text.Json;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Services;

public class FindApiAuthClientProvider : IFindApiAuthClientProvider
{
    private readonly Lazy<IReadOnlyList<AuthClient>> _authClients;

    public FindApiAuthClientProvider()
    {
        _authClients = new Lazy<IReadOnlyList<AuthClient>>(Load);
    }

    public IReadOnlyList<AuthClient> GetAuthClients() => _authClients.Value;

    private static IReadOnlyList<AuthClient> Load()
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
}
