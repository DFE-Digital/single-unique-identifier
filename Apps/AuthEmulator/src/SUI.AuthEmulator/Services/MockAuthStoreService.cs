using System.Text.Json;
using SUI.AuthEmulator.Models;

namespace SUI.AuthEmulator.Services;

public class MockAuthStoreService : IAuthStoreService
{
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
        var filePath = Path.Combine(baseDir, "Data", "auth-clients-inbound.json");

        if (!File.Exists(filePath))
        {
            throw new InvalidOperationException($"Auth store file not found at: {filePath}");
        }

        var json = await File.ReadAllTextAsync(filePath);
        var store = JsonSerializer.Deserialize<AuthStore>(json, JsonSerializerOptions.Web);

        if (store is null)
        {
            throw new InvalidOperationException("Auth store file could not be deserialized.");
        }
        return store;
    }
}
