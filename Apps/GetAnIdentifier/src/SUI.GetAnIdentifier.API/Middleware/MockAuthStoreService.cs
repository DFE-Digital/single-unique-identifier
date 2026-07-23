using System.IO.Abstractions;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SUI.GetAnIdentifier.Infrastructure.Models;

namespace SUI.GetAnIdentifier.API.Middleware;

public interface IAuthStoreService
{
    AuthClient? GetClientById(string clientId);
}

public class MockAuthStoreService : IAuthStoreService
{
    private readonly IFileSystem _fileSystem;
    private readonly IConfiguration _configuration;
    private readonly Lazy<AuthStore> _authStore;

    public MockAuthStoreService(IFileSystem fileSystem, IConfiguration configuration)
    {
        _fileSystem = fileSystem;
        _configuration = configuration;
        _authStore = new Lazy<AuthStore>(LoadStore);
    }

    public AuthClient? GetClientById(string clientId)
    {
        var store = _authStore.Value;

        var client = store.Clients?.FirstOrDefault(x => x.ClientId == clientId);

        return client;
    }

    private AuthStore LoadStore()
    {
        var baseDir = AppContext.BaseDirectory;
        var filePath = Path.Join(baseDir, "Data", "auth-clients-inbound.json");

        if (!_fileSystem.File.Exists(filePath))
        {
            throw new InvalidOperationException($"Auth store file not found at: {filePath}");
        }

        var json = _fileSystem.File.ReadAllText(filePath);
        var store = JsonSerializer.Deserialize<AuthStore>(json, JsonSerializerOptions.Web);

        if (store is null)
        {
            throw new InvalidOperationException("Auth store file could not be deserialized.");
        }

        foreach (var client in store.Clients ?? [])
        {
            var originalClientId = client.ClientId;
            client.ClientId =
                _configuration[$"AuthClientCredentials:{originalClientId}:NewClientId"]
                ?? client.ClientId;
        }

        return store;
    }
}
