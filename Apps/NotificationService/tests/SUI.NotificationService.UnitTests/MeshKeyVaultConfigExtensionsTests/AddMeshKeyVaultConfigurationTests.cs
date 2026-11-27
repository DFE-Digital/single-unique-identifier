using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.NotificationService.Mesh.Configuration;

namespace SUI.NotificationService.UnitTests.MeshKeyVaultConfigExtensionsTests;

public class AddMeshKeyVaultConfigurationTests
{
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

        configuration.AddMeshKeyVaultConfiguration(credential);

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

        configuration.AddMeshKeyVaultConfiguration(credential);

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
            configuration.AddMeshKeyVaultConfiguration(credential)
        );

        Assert.Contains("KeyVault:VaultUri", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ShouldThrowInvalidOperationException_WhenKeyVaultIsUnreachable()
    {
        using var configuration = CreateKeyVaultConfiguration();
        var innerException = new RequestFailedException(
            0,
            "Sensitive upstream network error",
            null,
            null
        );

        var exception = Assert.Throws<InvalidOperationException>(() =>
            configuration.AddMeshKeyVaultConfiguration(ClientThatThrows(innerException))
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
        var innerException = new RequestFailedException(
            403,
            "Sensitive upstream vault diagnostics and secret detail",
            "Forbidden",
            null
        );

        var exception = Assert.Throws<InvalidOperationException>(() =>
            configuration.AddMeshKeyVaultConfiguration(ClientThatThrows(innerException))
        );

        Assert.Equal("Key Vault request failed (HTTP 403, Forbidden).", exception.Message);
        Assert.DoesNotContain("Sensitive", exception.Message, StringComparison.Ordinal);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void ShouldThrowInvalidOperationException_WhenKeyVaultAuthenticationFails()
    {
        using var configuration = CreateKeyVaultConfiguration();
        var innerException = new AuthenticationFailedException(
            "Sensitive AAD/IMDS diagnostic payload"
        );

        var exception = Assert.Throws<InvalidOperationException>(() =>
            configuration.AddMeshKeyVaultConfiguration(ClientThatThrows(innerException))
        );

        Assert.Equal("Failed to authenticate with Key Vault.", exception.Message);
        Assert.DoesNotContain("Sensitive", exception.Message, StringComparison.Ordinal);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void ShouldThrowInvalidOperationException_WhenKeyVaultCredentialIsUnavailable()
    {
        using var configuration = CreateKeyVaultConfiguration();
        var innerException = new CredentialUnavailableException(
            "Sensitive managed identity payload"
        );

        var exception = Assert.Throws<InvalidOperationException>(() =>
            configuration.AddMeshKeyVaultConfiguration(ClientThatThrows(innerException))
        );

        Assert.Equal("Failed to authenticate with Key Vault.", exception.Message);
        Assert.DoesNotContain("Sensitive", exception.Message, StringComparison.Ordinal);
        Assert.Same(innerException, exception.InnerException);
    }

    private static SecretClient ClientThatThrows(Exception ex)
    {
        var client = Substitute.For<SecretClient>();
        client.VaultUri.Returns(new Uri("https://notificationkv01.vault.azure.net/"));
        client.GetPropertiesOfSecrets(Arg.Any<CancellationToken>()).Throws(ex);
        return client;
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
