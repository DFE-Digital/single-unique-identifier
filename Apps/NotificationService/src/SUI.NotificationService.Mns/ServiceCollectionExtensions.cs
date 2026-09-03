using Microsoft.Extensions.DependencyInjection;

namespace SUI.NotificationService.Mns;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Provides the composition point for MNS integration services. Concrete registrations are
    /// intentionally deferred until the MNS workstream is implemented.
    /// </summary>
    public static IServiceCollection AddMnsIntegration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services;
    }
}
