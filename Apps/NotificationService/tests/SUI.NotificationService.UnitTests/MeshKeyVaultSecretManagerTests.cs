using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
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
    public void ShouldSkipKeyVaultConfiguration_WhenVaultUriIsMissingOrEmpty(string? vaultUri)
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

    [Theory]
    [InlineData("not-a-uri")]
    [InlineData("http://notificationkv01.vault.azure.net/")]
    public void ShouldThrowInvalidOperationException_WhenVaultUriIsInvalidAndAcceptLocalDevCertIsFalse(
        string invalidVaultUri
    )
    {
        using var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                [$"{NhsMeshConfig.SectionName}:{nameof(NhsMeshConfig.AcceptLocalDevCert)}"] =
                    "false",
                ["KeyVault:VaultUri"] = invalidVaultUri,
            }
        );
        var credential = Substitute.For<TokenCredential>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            NotificationServiceHost.AddMeshKeyVaultConfiguration(configuration, credential)
        );

        Assert.Contains("KeyVault:VaultUri", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ShouldThrowInvalidOperationException_WhenKeyVaultIsUnreachable()
    {
        using var configuration = CreateKeyVaultConfiguration();
        var secretClient = Substitute.For<SecretClient>();
        var innerException = new RequestFailedException(
            0,
            "Sensitive upstream network error",
            null,
            null
        );
        secretClient.GetPropertiesOfSecrets(Arg.Any<CancellationToken>()).Throws(innerException);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            NotificationServiceHost.AddMeshKeyVaultConfiguration(configuration, _ => secretClient)
        );

        Assert.Equal(
            "Could not reach Key Vault at https://notificationkv01.vault.azure.net/.",
            exception.Message
        );
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void ShouldThrowInvalidOperationException_WhenKeyVaultRequestFails()
    {
        using var configuration = CreateKeyVaultConfiguration();
        var secretClient = Substitute.For<SecretClient>();
        var innerException = new RequestFailedException(
            403,
            "Sensitive upstream vault diagnostics and secret detail",
            "Forbidden",
            null
        );
        secretClient.GetPropertiesOfSecrets(Arg.Any<CancellationToken>()).Throws(innerException);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            NotificationServiceHost.AddMeshKeyVaultConfiguration(configuration, _ => secretClient)
        );

        Assert.Equal("Key Vault request failed (HTTP 403, Forbidden).", exception.Message);
        Assert.DoesNotContain("Sensitive", exception.Message, StringComparison.Ordinal);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void ShouldThrowInvalidOperationException_WhenKeyVaultAuthenticationFails()
    {
        using var configuration = CreateKeyVaultConfiguration();
        var secretClient = Substitute.For<SecretClient>();
        var innerException = new AuthenticationFailedException(
            "Sensitive AAD/IMDS diagnostic payload"
        );
        secretClient.GetPropertiesOfSecrets(Arg.Any<CancellationToken>()).Throws(innerException);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            NotificationServiceHost.AddMeshKeyVaultConfiguration(configuration, _ => secretClient)
        );

        Assert.Equal("Failed to authenticate with Key Vault.", exception.Message);
        Assert.DoesNotContain("Sensitive", exception.Message, StringComparison.Ordinal);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void ShouldThrowInvalidOperationException_WhenKeyVaultCredentialIsUnavailable()
    {
        using var configuration = CreateKeyVaultConfiguration();
        var secretClient = Substitute.For<SecretClient>();
        var innerException = new CredentialUnavailableException(
            "Sensitive managed identity payload"
        );
        secretClient.GetPropertiesOfSecrets(Arg.Any<CancellationToken>()).Throws(innerException);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            NotificationServiceHost.AddMeshKeyVaultConfiguration(configuration, _ => secretClient)
        );

        Assert.Equal("Failed to authenticate with Key Vault.", exception.Message);
        Assert.DoesNotContain("Sensitive", exception.Message, StringComparison.Ordinal);
        Assert.Same(innerException, exception.InnerException);
    }

    private static ConfigurationManager CreateKeyVaultConfiguration()
    {
        var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                [$"{NhsMeshConfig.SectionName}:{nameof(NhsMeshConfig.AcceptLocalDevCert)}"] =
                    "false",
                ["KeyVault:VaultUri"] = "https://notificationkv01.vault.azure.net/",
            }
        );
        return configuration;
    }
}
