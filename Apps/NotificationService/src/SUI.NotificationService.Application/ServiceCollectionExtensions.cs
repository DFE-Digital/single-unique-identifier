using Microsoft.Extensions.DependencyInjection;

namespace SUI.NotificationService.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationServiceApplication(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<INotificationOrchestrator, NotificationOrchestrator>();

        return services;
    }
}
