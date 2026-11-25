using System.Diagnostics.CodeAnalysis;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
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
    ) =>
        AddMeshKeyVaultConfiguration(
            configuration,
            vaultUri => new SecretClient(vaultUri, credential)
        );

    internal static void AddMeshKeyVaultConfiguration(
        IConfigurationManager configuration,
        Func<Uri, SecretClient> secretClientFactory
    )
    {
        if (!UseKeyVault(configuration, out var vaultUri))
        {
            return;
        }

        try
        {
            configuration.AddAzureKeyVault(
                secretClientFactory(vaultUri),
                new MeshKeyVaultSecretManager()
            );
        }
        catch (RequestFailedException ex) when (ex.Status == 0)
        {
            throw new InvalidOperationException($"Could not reach Key Vault at {vaultUri}.", ex);
        }
        catch (RequestFailedException ex)
        {
            throw new InvalidOperationException(
                $"Key Vault request failed (HTTP {ex.Status}, {ex.ErrorCode}).",
                ex
            );
        }
        catch (AuthenticationFailedException ex)
        {
            throw new InvalidOperationException("Failed to authenticate with Key Vault.", ex);
        }
    }

    private static bool UseKeyVault(
        IConfiguration configuration,
        [NotNullWhen(true)] out Uri? vaultUri
    )
    {
        vaultUri = null;

        var acceptLocalDevCert = configuration.GetValue<bool>(
            $"{NhsMeshConfig.SectionName}:{nameof(NhsMeshConfig.AcceptLocalDevCert)}"
        );
        if (acceptLocalDevCert)
        {
            return false;
        }

        var vaultUriValue = configuration["KeyVault:VaultUri"];
        if (string.IsNullOrWhiteSpace(vaultUriValue))
        {
            return false;
        }

        if (
            !Uri.TryCreate(vaultUriValue, UriKind.Absolute, out vaultUri)
            || vaultUri.Scheme != Uri.UriSchemeHttps
        )
        {
            throw new InvalidOperationException(
                "KeyVault:VaultUri must be an absolute https:// URI."
            );
        }

        return true;
    }
}
