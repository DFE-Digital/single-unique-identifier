using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
                return new TableServiceClient(tableServiceUri, new DefaultAzureCredential());
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
                return new TableServiceClient(connectionString);
            }

            throw new InvalidOperationException(
                "TableStorage:ServiceUri must be configured. "
                    + "UseDevelopmentStorage=true is supported only for local Azurite development."
            );
        });

        // Register the Repositories, passing in the ServiceClient so they can request their own table names
        services.AddSingleton<ISupplierWebhookRepository>(provider => new SupplierWebhookRepository(
            provider.GetRequiredService<TableServiceClient>().GetTableClient("SupplierWebhooks"),
            provider.GetRequiredService<ILogger<SupplierWebhookRepository>>()
        ));

        services.AddSingleton<IWebhookReceiptRepository>(provider => new WebhookReceiptRepository(
            provider.GetRequiredService<TableServiceClient>().GetTableClient("WebhookReceipts"),
            provider.GetRequiredService<ILogger<WebhookReceiptRepository>>()
        ));

        return services;
    }
}
