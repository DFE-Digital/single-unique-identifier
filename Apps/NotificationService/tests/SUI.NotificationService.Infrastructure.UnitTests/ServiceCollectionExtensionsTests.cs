using Azure.Core;
using Azure.Data.Tables;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

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

        var tableServiceClient = serviceProvider.GetRequiredService<TableServiceClient>();

        Assert.Equal(
            new Uri("https://notificationstorage.table.core.windows.net/"),
            tableServiceClient.Uri
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

        var tableServiceClient = serviceProvider.GetRequiredService<TableServiceClient>();

        Assert.Equal(new Uri("http://127.0.0.1:10002/devstoreaccount1"), tableServiceClient.Uri);
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
            serviceProvider.GetRequiredService<TableServiceClient>()
        );

        Assert.Contains("TableStorage:ServiceUri", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddNotificationServiceInfrastructure_ThrowsWhenTableStorageIsNotConfigured()
    {
        using var serviceProvider = BuildServiceProvider(new Dictionary<string, string?>());

        var exception = Assert.Throws<InvalidOperationException>(() =>
            serviceProvider.GetRequiredService<TableServiceClient>()
        );

        Assert.Contains("TableStorage:ServiceUri", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ShouldRegisterSecretClient_WhenKeyVaultVaultUriIsConfigured()
    {
        using var serviceProvider = BuildServiceProvider(
            new Dictionary<string, string?>
            {
                ["KeyVault:VaultUri"] = "https://notificationkv01.vault.azure.net/",
            }
        );

        var secretClient = serviceProvider.GetRequiredService<SecretClient>();

        Assert.Equal(new Uri("https://notificationkv01.vault.azure.net/"), secretClient.VaultUri);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ShouldNotRegisterSecretClient_WhenKeyVaultVaultUriIsMissingOrEmpty(string? vaultUri)
    {
        using var serviceProvider = BuildServiceProvider(
            new Dictionary<string, string?> { ["KeyVault:VaultUri"] = vaultUri }
        );

        var secretClient = serviceProvider.GetService<SecretClient>();

        Assert.Null(secretClient);
    }

    [Theory]
    [InlineData("not-a-uri")]
    [InlineData("http://notificationkv01.vault.azure.net/")]
    public void ShouldThrow_WhenKeyVaultVaultUriIsInvalid(string invalidUri)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            BuildServiceProvider(
                new Dictionary<string, string?> { ["KeyVault:VaultUri"] = invalidUri }
            )
        );

        Assert.Contains("KeyVault:VaultUri", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ShouldUseProvidedTokenCredential_WhenPassedToAddNotificationServiceInfrastructure()
    {
        var credential = Substitute.For<TokenCredential>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["KeyVault:VaultUri"] = "https://notificationkv01.vault.azure.net/",
                }
            )
            .Build();
        var services = new ServiceCollection();
        services.AddNotificationServiceInfrastructure(configuration, credential);

        using var serviceProvider = services.BuildServiceProvider();

        Assert.Same(credential, serviceProvider.GetRequiredService<TokenCredential>());
    }

    private static ServiceProvider BuildServiceProvider(IReadOnlyDictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddNotificationServiceInfrastructure(configuration);

        return services.BuildServiceProvider();
    }
}
