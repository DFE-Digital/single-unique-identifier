using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Services;

public class FindApiAuthClientProvider : IFindApiAuthClientProvider
{
    private readonly IConfiguration _configuration;
    private readonly Lazy<IReadOnlyList<AuthClient>> _authClients;

    public FindApiAuthClientProvider(IConfiguration configuration)
    {
        _configuration = configuration;
        _authClients = new Lazy<IReadOnlyList<AuthClient>>(Load);
    }

    public IReadOnlyList<AuthClient> GetAuthClients() => _authClients.Value;

    private IReadOnlyList<AuthClient> Load()
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

        foreach (var client in store.Clients ?? [])
        {
            var originalClientId = client.ClientId;
            client.ClientId =
                _configuration[$"AuthClientCredentials:{originalClientId}:NewClientId"]
                ?? client.ClientId;
            client.ClientSecret =
                _configuration[$"AuthClientCredentials:{originalClientId}:NewClientSecret"]
                ?? client.ClientSecret;
        }

        return store.Clients ?? [];
    }
}
