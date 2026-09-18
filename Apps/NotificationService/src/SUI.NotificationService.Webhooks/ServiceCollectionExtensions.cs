using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SUI.NotificationService.Application.Interfaces;

namespace SUI.NotificationService.Webhooks;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the supplier webhook delivery services and their dependencies,
    /// including the specifically configured HTTP client, Key Vault secret client,
    /// and time providers required for secure webhook dispatch.
    /// </summary>
    public static IServiceCollection AddSupplierWebhookDelivery(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Secret Retrieval & Caching
        services.AddMemoryCache();

        services.AddSingleton(sp =>
        {
            var keyVaultUri =
                configuration.GetValue<string>("KeyVault:VaultUri")
                ?? throw new InvalidOperationException(
                    "KeyVault:VaultUri configuration is missing."
                );

            return new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
        });

        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<IWebhookSecretClient, WebhookSecretClient>();

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
