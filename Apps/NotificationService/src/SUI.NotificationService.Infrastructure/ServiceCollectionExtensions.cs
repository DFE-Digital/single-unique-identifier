using Microsoft.Extensions.DependencyInjection;

namespace SUI.NotificationService.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Provides the composition point for infrastructure shared by the Notification Service
    /// modules. Concrete registrations are intentionally deferred until they are required.
    /// </summary>
    public static IServiceCollection AddNotificationServiceInfrastructure(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);

        return services;
    }
}
