using Microsoft.Extensions.DependencyInjection;

namespace SUI.NotificationService.Webhooks;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSupplierWebhookDelivery(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services;
    }
}
