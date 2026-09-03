using Microsoft.Extensions.DependencyInjection;

namespace SUI.NotificationService.Mns;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMnsIntegration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services;
    }
}
