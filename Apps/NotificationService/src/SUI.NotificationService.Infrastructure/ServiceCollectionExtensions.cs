using Azure.Data.Tables;
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

        // Get the connection string
        var connectionString =
            configuration.GetConnectionString("TableStorage")
            ?? throw new InvalidOperationException(
                "TableStorage connection string is missing from configuration."
            );

        // Register the Azure TableClient specifically for our webhooks table
        services.AddSingleton(serviceProvider =>
        {
            var tableClient = new TableClient(connectionString, "SupplierWebhooks");

            // Ensures the table is created on startup if it doesn't exist in the storage account
            tableClient.CreateIfNotExists();

            return tableClient;
        });

        // Register the Repository
        services.AddSingleton<ISupplierWebhookRepository, SupplierWebhookRepository>();

        return services;
    }
}
