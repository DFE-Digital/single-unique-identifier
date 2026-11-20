using Azure;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SUI.NotificationService.Mesh.Configuration;

namespace SUI.NotificationService;

/// <summary>
/// Populates NHS MESH secrets (currently <see cref="NhsMeshConfig.MailboxMTLSPfx"/>) from Azure
/// Key Vault at runtime when <see cref="NhsMeshConfig.AcceptLocalDevCert"/> is false and the value
/// was not supplied directly via local configuration (e.g. user-secrets).
/// </summary>
internal sealed class KeyVaultMeshSecretsPostConfigure(IServiceProvider serviceProvider)
    : IPostConfigureOptions<NhsMeshConfig>
{
    public void PostConfigure(string? name, NhsMeshConfig options)
    {
        if (
            options.AcceptLocalDevCert
            || !string.IsNullOrWhiteSpace(options.MailboxMTLSPfx)
            || string.IsNullOrWhiteSpace(options.MailboxMTLSPfxCertName)
        )
        {
            return;
        }

        var secretClient = serviceProvider.GetRequiredService<SecretClient>();

        try
        {
            var response = secretClient.GetSecret(options.MailboxMTLSPfxCertName);
            options.MailboxMTLSPfx = response.Value.Value;
        }
        catch (RequestFailedException ex)
        {
            throw new InvalidOperationException(
                $"Failed to retrieve MESH client certificate '{options.MailboxMTLSPfxCertName}' from Key Vault (HTTP {ex.Status})."
            );
        }
    }
}
