using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SUI.NotificationService.Application.Interfaces;

namespace SUI.NotificationService.Webhooks.UnitTests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSupplierWebhookDelivery_ResolvesDependenciesCorrectly_WhenSecretClientIsRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<SecretClient>());

        // Act
        services.AddSupplierWebhookDelivery();

        // Assert
        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }
        );

        var service = provider.GetRequiredService<ISupplierWebhookDeliveryService>();
        Assert.IsType<SupplierWebhookDeliveryService>(service);
    }

    [Fact]
    public void AddSupplierWebhookDelivery_ResolvesDependenciesCorrectly_WhenSecretClientIsNotRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSupplierWebhookDelivery();

        // Assert
        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }
        );

        var service = provider.GetRequiredService<ISupplierWebhookDeliveryService>();
        Assert.IsType<SupplierWebhookDeliveryService>(service);
    }

    [Fact]
    public void AddSupplierWebhookDelivery_PreservesExistingTimeProvider_WhenAlreadyRegistered()
    {
        // Arrange
        var customTimeProvider = Substitute.For<TimeProvider>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(customTimeProvider);

        // Act
        services.AddSupplierWebhookDelivery();

        // Assert
        using var provider = services.BuildServiceProvider();
        Assert.Same(customTimeProvider, provider.GetRequiredService<TimeProvider>());
    }

    [Fact]
    public void AddSupplierWebhookDelivery_ConfiguresHttpClient_WithNoAutoRedirect()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSupplierWebhookDelivery();

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
