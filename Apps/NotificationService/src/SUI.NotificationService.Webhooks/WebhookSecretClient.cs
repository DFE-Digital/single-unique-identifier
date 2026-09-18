using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SUI.NotificationService.Application.Interfaces;

namespace SUI.NotificationService.Webhooks;

public class WebhookSecretClient : IWebhookSecretClient
{
    private readonly SecretClient _secretClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WebhookSecretClient> _logger;

    // Cache the secret to prevent throttling and reduce latency on high-volume webhooks
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(4);

    public WebhookSecretClient(
        SecretClient secretClient,
        IMemoryCache cache,
        ILogger<WebhookSecretClient> logger
    )
    {
        _secretClient = secretClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> GetSecretBase64Async(
        string keyVaultReference,
        CancellationToken cancellationToken = default
    )
    {
        var cacheKey = $"webhook-secret-{keyVaultReference}";

        // Try to get it from local memory first (no latency)
        if (_cache.TryGetValue(cacheKey, out string? cachedSecret) && cachedSecret != null)
        {
            return cachedSecret;
        }

        try
        {
            // Fallback to Azure Key Vault if not cached
            KeyVaultSecret secret = await _secretClient.GetSecretAsync(
                keyVaultReference,
                cancellationToken: cancellationToken
            );
            var secretValue = secret.Value;

            // Store in short term cache for future webhook deliveries
            _cache.Set(cacheKey, secretValue, CacheDuration);

            return secretValue;
        }
        catch (OperationCanceledException)
        {
            // Prevent task cancellations from being incorrectly wrapped up as Key Vault failures
            throw;
        }
        catch (Exception ex)
        {
            // Wrap the exception with context
            throw new InvalidOperationException(
                $"Failed to retrieve webhook secret from Key Vault for reference: {keyVaultReference}",
                ex
            );
        }
    }
}
