using System.Text.Json;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Services;

public class AuthClientProvider : IAuthClientProvider
{
    private readonly Lazy<IReadOnlyList<AuthClient>> _authClients;

    public AuthClientProvider()
    {
        _authClients = new Lazy<IReadOnlyList<AuthClient>>(Load);
    }

    public IReadOnlyList<AuthClient> GetAuthClients() => _authClients.Value;

    private static IReadOnlyList<AuthClient> Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "auth-clients.json");

        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"auth-clients.json not found: {path}");
        }

        var json = File.ReadAllText(path);

        var data =
            JsonSerializer.Deserialize<AuthStore>(json, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException("Invalid auth-clients.json");

        return data.Clients ?? [];
    }
}
