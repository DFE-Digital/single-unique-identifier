using System.IO.Abstractions;
using System.Text.Json;
using SUI.Find.Infrastructure.Models;

namespace SUI.Find.Infrastructure.Services;

public interface IAuthStoreService
{
    Task<AuthStore> GetAuthStoreAsync();
    Task<Result<AuthClient>> GetClientByCredentials(string clientId, string clientSecret);
}

public class FileAuthStoreService(IFileSystem fileSystem) : IAuthStoreService
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<AuthStore> GetAuthStoreAsync()
    {
        return await GetStore();
    }

    public async Task<Result<AuthClient>> GetClientByCredentials(
        string clientId,
        string clientSecret
    )
    {
        var store = await GetStore();

        if (
            string.IsNullOrWhiteSpace(store.Issuer)
            || string.IsNullOrWhiteSpace(store.Audience)
            || string.IsNullOrWhiteSpace(store.SigningKey)
        )
        {
            // May be better to log this error instead of throwing, doesn't stop the service
            throw new InvalidOperationException(
                "Auth store file is missing issuer, audience, or signingKey."
            );
        }

        store.Clients ??= [];

        var client = store.Clients.FirstOrDefault(c =>
            c.ClientId == clientId && c.ClientSecret == clientSecret && c.Enabled
        );

        return client is null
            ? Result<AuthClient>.Fail("Unauthorized")
            : Result<AuthClient>.Ok(client);
    }

    private async Task<AuthStore> GetStore()
    {
        var baseDir = AppContext.BaseDirectory;
        var filePath = Path.Combine(baseDir, "Data", "auth-clients.json");

        if (!File.Exists(filePath))
        {
            throw new InvalidOperationException($"Auth store file not found at: {filePath}");
        }

        var json = await fileSystem.File.ReadAllTextAsync(filePath);
        var store = JsonSerializer.Deserialize<AuthStore>(json, _jsonSerializerOptions);

        if (store is null)
        {
            throw new InvalidOperationException("Auth store file could not be deserialized.");
        }
        return store;
    }
}
