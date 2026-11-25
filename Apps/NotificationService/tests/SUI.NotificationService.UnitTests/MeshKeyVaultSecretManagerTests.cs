using Azure.Security.KeyVault.Secrets;

namespace SUI.NotificationService.UnitTests;

public sealed class MeshKeyVaultSecretManagerTests
{
    [Theory]
    [InlineData("nhs-mesh-shared-key")]
    [InlineData("nhs-mesh-mailbox-id")]
    [InlineData("nhs-mesh-mailbox-password")]
    [InlineData("nhs-mesh-mailbox-certificate")]
    [InlineData("NHS-MESH-SHARED-KEY")]
    [InlineData("Nhs-Mesh-Mailbox-Certificate")]
    public void ShouldLoadSecret_WhenNameIsAllowedMeshSecret(string secretName)
    {
        var sut = new MeshKeyVaultSecretManager();

        Assert.True(sut.Load(new SecretProperties(secretName)));
    }

    [Theory]
    [InlineData("supplier-a-hmac-secret")]
    [InlineData("webhook-signing-key")]
    [InlineData("nhs-mesh-root-ca")]
    [InlineData("unrelated-secret")]
    public void ShouldIgnoreSecret_WhenNameIsNotAllowedMeshSecret(string secretName)
    {
        var sut = new MeshKeyVaultSecretManager();

        Assert.False(sut.Load(new SecretProperties(secretName)));
    }

    [Theory]
    [InlineData("nhs-mesh-shared-key", "NhsMeshConfig:SharedKey")]
    [InlineData("nhs-mesh-mailbox-id", "NhsMeshConfig:MailboxId")]
    [InlineData("nhs-mesh-mailbox-password", "NhsMeshConfig:MailboxPassword")]
    [InlineData("nhs-mesh-mailbox-certificate", "NhsMeshConfig:MailboxMTLSPfx")]
    [InlineData("NHS-MESH-MAILBOX-ID", "NhsMeshConfig:MailboxId")]
    public void ShouldMapSecretNameToNhsMeshConfigKey_WhenGettingKey(
        string secretName,
        string expectedKey
    )
    {
        var sut = new MeshKeyVaultSecretManager();

        var key = sut.GetKey(new KeyVaultSecret(secretName, "secret-value"));

        Assert.Equal(expectedKey, key);
    }
}
