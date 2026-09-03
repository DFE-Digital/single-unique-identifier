using Microsoft.Extensions.DependencyInjection;

namespace SUI.NotificationService.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationServiceInfrastructure(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);

        return services;
    }
}
