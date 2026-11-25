using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;
using SUI.NotificationService.Mesh.Configuration;

namespace SUI.NotificationService;

internal sealed class MeshKeyVaultSecretManager : KeyVaultSecretManager
{
    private const string SharedKeySecretName = "nhs-mesh-shared-key";
    private const string MailboxIdSecretName = "nhs-mesh-mailbox-id";
    private const string MailboxPasswordSecretName = "nhs-mesh-mailbox-password";
    private const string MailboxMtlsPfxSecretName = "nhs-mesh-mailbox-certificate";

    private static readonly Dictionary<string, string> SecretMap = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        [SharedKeySecretName] = $"{NhsMeshConfig.SectionName}:{nameof(NhsMeshConfig.SharedKey)}",
        [MailboxIdSecretName] = $"{NhsMeshConfig.SectionName}:{nameof(NhsMeshConfig.MailboxId)}",
        [MailboxPasswordSecretName] =
            $"{NhsMeshConfig.SectionName}:{nameof(NhsMeshConfig.MailboxPassword)}",
        [MailboxMtlsPfxSecretName] =
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
