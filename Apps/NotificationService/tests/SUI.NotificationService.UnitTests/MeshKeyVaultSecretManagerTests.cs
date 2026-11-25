using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using SUI.NotificationService.Mesh.Configuration;

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

    [Fact]
    public void ShouldSkipKeyVaultConfiguration_WhenAcceptLocalDevCertIsTrue()
    {
        using var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                [$"{NhsMeshConfig.SectionName}:{nameof(NhsMeshConfig.AcceptLocalDevCert)}"] =
                    "true",
                ["KeyVault:VaultUri"] = "https://notificationkv01.vault.azure.net/",
            }
        );
        var initialSourceCount = configuration.Sources.Count;
        var credential = Substitute.For<TokenCredential>();

        NotificationServiceHost.AddMeshKeyVaultConfiguration(configuration, credential);

        Assert.Equal(initialSourceCount, configuration.Sources.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-uri")]
    [InlineData("http://notificationkv01.vault.azure.net/")]
    public void ShouldSkipKeyVaultConfiguration_WhenVaultUriIsMissingOrNotHttps(string? vaultUri)
    {
        using var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                [$"{NhsMeshConfig.SectionName}:{nameof(NhsMeshConfig.AcceptLocalDevCert)}"] =
                    "false",
                ["KeyVault:VaultUri"] = vaultUri,
            }
        );
        var initialSourceCount = configuration.Sources.Count;
        var credential = Substitute.For<TokenCredential>();

        NotificationServiceHost.AddMeshKeyVaultConfiguration(configuration, credential);

        Assert.Equal(initialSourceCount, configuration.Sources.Count);
    }
}
