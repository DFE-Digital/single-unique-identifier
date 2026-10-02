using Microsoft.Extensions.DependencyInjection;
using SUI.NotificationService.Application.Services;

namespace SUI.NotificationService.Application;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers application orchestration. Integration contracts introduced by later
    /// workstreams belong here so orchestration remains independent of adapter implementations.
    /// </summary>
    public static IServiceCollection AddNotificationServiceApplication(
        this IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<INotificationOrchestrator, NotificationOrchestrator>();
        services.AddScoped<IMeshMessageProcessor, MeshMessageProcessor>();

        return services;
    }
}
