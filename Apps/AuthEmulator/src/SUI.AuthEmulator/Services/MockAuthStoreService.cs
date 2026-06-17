using System.IO.Abstractions;
using System.Text.Json;
using SUI.AuthEmulator.Models;

namespace SUI.AuthEmulator.Services;

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

    public Task<Result<AuthClient>> GetClientByCredentials(string clientId, string clientSecret)
    {
        var store = _authStore.Value;

        store.Clients ??= [];

        var client = store.Clients.FirstOrDefault(c =>
            c.ClientId == clientId && c.ClientSecret == clientSecret && c.Enabled
        );

        var result = client is null
            ? Result<AuthClient>.Fail("Unauthorized")
            : Result<AuthClient>.Ok(client);

        return Task.FromResult(result);
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
            client.ClientSecret =
                _configuration[$"AuthClientCredentials:{originalClientId}:NewClientSecret"]
                ?? client.ClientSecret;
        }

        return store;
    }
}
