using Microsoft.Extensions.DependencyInjection;

namespace SUI.NotificationService.Webhooks;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Provides the composition point for supplier webhook delivery services. Concrete
    /// registrations are intentionally deferred until the webhook workstream is implemented.
    /// </summary>
    public static IServiceCollection AddSupplierWebhookDelivery(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services;
    }
}
