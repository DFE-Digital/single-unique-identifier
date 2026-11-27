using System.Diagnostics.CodeAnalysis;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using SUI.NotificationService.Mesh.Configuration;

namespace SUI.NotificationService;

internal static class MeshKeyVaultConfigExtensions
{
    internal static IConfigurationManager AddMeshKeyVaultConfiguration(
        this IConfigurationManager configuration,
        TokenCredential credential
    )
    {
        if (!UseKeyVault(configuration, out var vaultUri))
        {
            return configuration;
        }

        return configuration.AddMeshKeyVaultConfiguration(new SecretClient(vaultUri, credential));
    }

    internal static IConfigurationManager AddMeshKeyVaultConfiguration(
        this IConfigurationManager configuration,
        SecretClient secretClient
    )
    {
        var vaultUri = secretClient.VaultUri;

        try
        {
            configuration.AddAzureKeyVault(secretClient, new MeshKeyVaultSecretManager());
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

        return configuration;
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
