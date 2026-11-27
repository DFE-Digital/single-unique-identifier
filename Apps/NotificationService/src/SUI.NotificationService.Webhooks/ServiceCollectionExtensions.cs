using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SUI.NotificationService.Application.Interfaces;

namespace SUI.NotificationService.Webhooks;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the supplier webhook delivery services and their dependencies,
    /// including the specifically configured HTTP client, Key Vault secret client wrapper,
    /// and time providers required for secure webhook dispatch.
    /// </summary>
    public static IServiceCollection AddSupplierWebhookDelivery(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Secret Retrieval & Caching
        services.AddMemoryCache();

        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<IWebhookSecretClient>(sp => new WebhookSecretClient(
            sp.GetService<SecretClient>(),
            sp.GetRequiredService<IMemoryCache>()
        ));

        // HTTP Client Configuration
        services
            .AddHttpClient<ISupplierWebhookDeliveryService, SupplierWebhookDeliveryService>(
                client =>
                {
                    // The webhook contract specifies a strict 3-second timeout for acknowledgements
                    client.Timeout = TimeSpan.FromSeconds(3);
                }
            )
            .ConfigurePrimaryHttpMessageHandler(() =>
                new SocketsHttpHandler
                {
                    // Contract specifies 3xx responses are non-retryable contract failures. Redirects must not be followed.
                    AllowAutoRedirect = false,
                }
            );

        return services;
    }
}
