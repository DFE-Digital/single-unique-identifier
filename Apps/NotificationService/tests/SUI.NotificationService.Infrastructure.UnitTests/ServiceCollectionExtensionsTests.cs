using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SUI.NotificationService.Infrastructure;

namespace SUI.NotificationService.Infrastructure.UnitTests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddNotificationServiceInfrastructure_UsesManagedIdentityForServiceUri()
    {
        using var serviceProvider = BuildServiceProvider(
            new Dictionary<string, string?>
            {
                ["TableStorage:ServiceUri"] = "https://notificationstorage.table.core.windows.net",
            }
        );

        var tableClient = serviceProvider.GetRequiredService<TableClient>();

        Assert.Equal(
            new Uri("https://notificationstorage.table.core.windows.net/SupplierWebhooks"),
            tableClient.Uri
        );
    }

    [Fact]
    public void AddNotificationServiceInfrastructure_UsesAzuriteForDevelopmentStorageConnectionString()
    {
        using var serviceProvider = BuildServiceProvider(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:TableStorage"] = "UseDevelopmentStorage=true",
            }
        );

        var tableClient = serviceProvider.GetRequiredService<TableClient>();

        Assert.Equal(
            new Uri("http://127.0.0.1:10002/devstoreaccount1/SupplierWebhooks"),
            tableClient.Uri
        );
    }

    [Fact]
    public void AddNotificationServiceInfrastructure_RejectsNonAzuriteConnectionStrings()
    {
        using var serviceProvider = BuildServiceProvider(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:TableStorage"] =
                    "DefaultEndpointsProtocol=https;AccountName=storage",
            }
        );

        var exception = Assert.Throws<InvalidOperationException>(() =>
            serviceProvider.GetRequiredService<TableClient>()
        );

        Assert.Contains("TableStorage:ServiceUri", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddNotificationServiceInfrastructure_ThrowsWhenTableStorageIsNotConfigured()
    {
        using var serviceProvider = BuildServiceProvider(new Dictionary<string, string?>());

        var exception = Assert.Throws<InvalidOperationException>(() =>
            serviceProvider.GetRequiredService<TableClient>()
        );

        Assert.Contains("TableStorage:ServiceUri", exception.Message, StringComparison.Ordinal);
    }

    private static ServiceProvider BuildServiceProvider(IReadOnlyDictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddNotificationServiceInfrastructure(configuration);

        return services.BuildServiceProvider();
    }
}
