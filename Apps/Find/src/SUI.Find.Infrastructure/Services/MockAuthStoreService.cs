using System.IO.Abstractions;
using System.Text.Json;
using SUI.Find.Infrastructure.Models;

namespace SUI.Find.Infrastructure.Services;

public interface IAuthStoreService
{
    Task<AuthStore> GetAuthStoreAsync();
    IReadOnlyList<string> GetScopesByClientId(string clientId);
    string GetOrganisationIdForClientId(string clientId);
}

public class MockAuthStoreService : IAuthStoreService
{
    private readonly IFileSystem _fileSystem;
    private readonly Lazy<AuthStore> _authStore;

    public MockAuthStoreService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
        _authStore = new Lazy<AuthStore>(LoadStore);
    }

    public Task<AuthStore> GetAuthStoreAsync()
    {
        // Instantly returns the cached in-memory store as a completed Task
        return Task.FromResult(_authStore.Value);
    }

    public IReadOnlyList<string> GetScopesByClientId(string clientId)
    {
        var clients = GetClients();

        var scopes = clients.FirstOrDefault(x => x.ClientId == clientId)?.AllowedScopes ?? [];
        return scopes;
    }

    public string GetOrganisationIdForClientId(string clientId)
    {
        var clients = GetClients();

        var organisationId =
            clients.FirstOrDefault(x => x.ClientId == clientId)?.OrganisationId ?? string.Empty;
        return organisationId;
    }

    private List<AuthClient> GetClients()
    {
        var store = _authStore.Value;

        if (
            string.IsNullOrWhiteSpace(store.Issuer)
            || string.IsNullOrWhiteSpace(store.Audience)
            || string.IsNullOrWhiteSpace(store.SigningKey)
        )
        {
            throw new InvalidOperationException(
                "Auth store file is missing issuer, audience, or signingKey."
            );
        }

        store.Clients ??= [];
        return store.Clients;
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

        return store;
    }
}
