using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SUI.NotificationService.Application.Interfaces;
using SUI.NotificationService.Infrastructure.Repositories;

namespace SUI.NotificationService.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Provides the composition point for infrastructure shared by the Notification Service
    /// modules. Concrete registrations are intentionally deferred until they are required.
    /// </summary>
    public static IServiceCollection AddNotificationServiceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(serviceProvider =>
        {
            var serviceUri = configuration["TableStorage:ServiceUri"];
            if (Uri.TryCreate(serviceUri, UriKind.Absolute, out var tableServiceUri))
            {
                return new TableClient(
                    tableServiceUri,
                    "SupplierWebhooks",
                    new DefaultAzureCredential()
                );
            }

            var connectionString = configuration.GetConnectionString("TableStorage");
            if (
                string.Equals(
                    connectionString,
                    "UseDevelopmentStorage=true",
                    StringComparison.Ordinal
                )
            )
            {
                return new TableClient(connectionString, "SupplierWebhooks");
            }

            throw new InvalidOperationException(
                "TableStorage:ServiceUri must be configured. "
                    + "UseDevelopmentStorage=true is supported only for local Azurite development."
            );
        });

        // Register the Repository
        services.AddSingleton<ISupplierWebhookRepository, SupplierWebhookRepository>();

        return services;
    }
}
