using Azure;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SUI.NotificationService.Application;
using SUI.NotificationService.Infrastructure;
using SUI.NotificationService.Mesh;
using SUI.NotificationService.Mesh.Configuration;
using SUI.NotificationService.Webhooks;

namespace SUI.NotificationService;

internal static class NotificationServiceHost
{
    public static IHost Build(string[] args)
    {
        var settings = new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory,
        };
        var builder = Host.CreateApplicationBuilder(settings);

        TokenCredential credential = new DefaultAzureCredential();

        AddMeshKeyVaultConfiguration(builder.Configuration, credential);

        builder.Services.AddNotificationServiceApplication();
        builder.Services.AddMeshIntegration();
        builder.Services.AddSupplierWebhookDelivery();
        builder.Services.AddNotificationServiceInfrastructure(builder.Configuration, credential);

        return builder.Build();
    }

    internal static void AddMeshKeyVaultConfiguration(
        IConfigurationManager configuration,
        TokenCredential credential
    )
    {
        var acceptLocalDevCert = configuration.GetValue<bool>(
            $"{NhsMeshConfig.SectionName}:{nameof(NhsMeshConfig.AcceptLocalDevCert)}"
        );
        if (acceptLocalDevCert)
        {
            return;
        }

        var vaultUriValue = configuration["KeyVault:VaultUri"];
        if (
            !Uri.TryCreate(vaultUriValue, UriKind.Absolute, out var vaultUri)
            || vaultUri.Scheme != Uri.UriSchemeHttps
        )
        {
            return;
        }

        try
        {
            configuration.AddAzureKeyVault(vaultUri, credential, new MeshKeyVaultSecretManager());
        }
        catch (RequestFailedException ex)
        {
            throw new InvalidOperationException(
                $"Failed to load MESH secrets from Key Vault (HTTP {ex.Status})."
            );
        }
    }
}
