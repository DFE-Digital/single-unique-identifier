using System.IO.Abstractions;
using System.Text.Json;
using SUI.Find.Infrastructure.Models;

namespace SUI.Find.Infrastructure.Services;

public interface IAuthStoreService
{
    Task<AuthStore> GetAuthStore();
}

public class AuthStoreService(IFileSystem fileSystem) : IAuthStoreService
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<AuthStore> GetAuthStore()
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
            throw new InvalidOperationException("Auth store file could not be deserialised.");
        }

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

        return store;
    }
}
