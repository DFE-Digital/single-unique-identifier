using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;
using SUI.NotificationService.Mesh.Configuration;

namespace SUI.NotificationService;

internal sealed class MeshKeyVaultSecretManager : KeyVaultSecretManager
{
    private static readonly Dictionary<string, string> SecretMap = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["nhs-mesh-shared-key"] = $"{NhsMeshConfig.SectionName}:{nameof(NhsMeshConfig.SharedKey)}",
        ["nhs-mesh-mailbox-id"] = $"{NhsMeshConfig.SectionName}:{nameof(NhsMeshConfig.MailboxId)}",
        ["nhs-mesh-mailbox-password"] =
            $"{NhsMeshConfig.SectionName}:{nameof(NhsMeshConfig.MailboxPassword)}",
        ["nhs-mesh-mailbox-certificate"] =
            $"{NhsMeshConfig.SectionName}:{nameof(NhsMeshConfig.MailboxMTLSPfx)}",
    };

    public override bool Load(SecretProperties secret)
    {
        ArgumentNullException.ThrowIfNull(secret);
        return SecretMap.ContainsKey(secret.Name);
    }

    public override string GetKey(KeyVaultSecret secret)
    {
        ArgumentNullException.ThrowIfNull(secret);
        return SecretMap.TryGetValue(secret.Name, out var key) ? key : base.GetKey(secret);
    }
}
