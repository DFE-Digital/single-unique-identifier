using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SUI.NotificationService.Application.Interfaces;

namespace SUI.NotificationService.Webhooks.UnitTests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSupplierWebhookDelivery_ResolvesDependenciesCorrectly()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["KeyVault:VaultUri"] = "https://test-vault.vault.azure.net/",
                }
            )
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSupplierWebhookDelivery(configuration);

        // Assert
        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }
        );

        // This will throw if TimeProvider, KeyVault, or any nested dependency is missing
        var service = provider.GetRequiredService<ISupplierWebhookDeliveryService>();
        Assert.IsType<SupplierWebhookDeliveryService>(service);
    }

    [Fact]
    public void AddSupplierWebhookDelivery_ConfiguresHttpClient_WithNoAutoRedirect()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["KeyVault:VaultUri"] = "https://test-vault.vault.azure.net/",
                }
            )
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSupplierWebhookDelivery(configuration);

        // Assert
        using var provider = services.BuildServiceProvider();

        // Typed HTTP clients are registered in DI using the name of the interface type
        var factory = provider.GetRequiredService<IHttpMessageHandlerFactory>();
        var handler = factory.CreateHandler(nameof(ISupplierWebhookDeliveryService));

        // Dig through any delegating handlers to find the primary transport handler at the bottom of the pipeline
        var currentHandler = handler;
        while (currentHandler is DelegatingHandler delegatingHandler)
        {
            currentHandler = delegatingHandler.InnerHandler!;
        }

        // Verify the primary handler explicitly disables redirects to protect the contract requirement
        var primaryHandler = Assert.IsType<SocketsHttpHandler>(currentHandler);
        Assert.False(
            primaryHandler.AllowAutoRedirect,
            "HTTP Client should be configured to NOT follow redirects."
        );
    }
}
