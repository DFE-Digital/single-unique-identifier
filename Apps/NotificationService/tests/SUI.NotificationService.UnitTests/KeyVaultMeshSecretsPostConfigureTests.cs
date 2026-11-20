using Azure;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SUI.NotificationService.Mesh.Configuration;

namespace SUI.NotificationService.UnitTests;

public sealed class KeyVaultMeshSecretsPostConfigureTests
{
    private static NhsMeshConfig CreateConfig(
        bool acceptLocalDevCert = false,
        string? mailboxMtlsPfx = null,
        string mailboxMtlsPfxCertName = "MailboxMTLSPfx"
    ) =>
        new()
        {
            MailboxBaseUrl = "https://msg.intspineservices.nhs.uk",
            SharedKey = "shared-key",
            MailboxId = "X26ABC1",
            MailboxPassword = "password",
            AcceptLocalDevCert = acceptLocalDevCert,
            MailboxMTLSPfx = mailboxMtlsPfx,
            MailboxMTLSPfxCertName = mailboxMtlsPfxCertName,
        };

    [Fact]
    public void ShouldSkipKeyVault_WhenAcceptLocalDevCertIsTrue()
    {
        var secretClient = Substitute.For<SecretClient>();
        using var provider = new ServiceCollection()
            .AddSingleton(secretClient)
            .BuildServiceProvider();
        var sut = new KeyVaultMeshSecretsPostConfigure(provider);
        var config = CreateConfig(acceptLocalDevCert: true);

        sut.PostConfigure(null, config);

        Assert.Null(config.MailboxMTLSPfx);
        secretClient.DidNotReceiveWithAnyArgs().GetSecret(default!);
    }

    [Fact]
    public void ShouldSkipKeyVault_WhenMailboxMTLSPfxAlreadyConfigured()
    {
        var secretClient = Substitute.For<SecretClient>();
        using var provider = new ServiceCollection()
            .AddSingleton(secretClient)
            .BuildServiceProvider();
        var sut = new KeyVaultMeshSecretsPostConfigure(provider);
        var config = CreateConfig(mailboxMtlsPfx: "local-pfx-base64");

        sut.PostConfigure(null, config);

        Assert.Equal("local-pfx-base64", config.MailboxMTLSPfx);
        secretClient.DidNotReceiveWithAnyArgs().GetSecret(default!);
    }

    [Fact]
    public void ShouldSkipKeyVault_WhenCertNameIsEmpty()
    {
        var secretClient = Substitute.For<SecretClient>();
        using var provider = new ServiceCollection()
            .AddSingleton(secretClient)
            .BuildServiceProvider();
        var sut = new KeyVaultMeshSecretsPostConfigure(provider);
        var config = CreateConfig(mailboxMtlsPfxCertName: "");

        sut.PostConfigure(null, config);

        Assert.Null(config.MailboxMTLSPfx);
        secretClient.DidNotReceiveWithAnyArgs().GetSecret(default!);
    }

    [Fact]
    public void ShouldPopulateMailboxMTLSPfx_WhenFetchedFromKeyVault()
    {
        const string certName = "MailboxMTLSPfx";
        const string expectedPfxBase64 = "cGZ4LWJ5dGVz";
        var secretClient = Substitute.For<SecretClient>();
        var response = Response.FromValue(
            new KeyVaultSecret(certName, expectedPfxBase64),
            Substitute.For<Response>()
        );
        secretClient.GetSecret(certName).Returns(response);

        using var provider = new ServiceCollection()
            .AddSingleton(secretClient)
            .BuildServiceProvider();
        var sut = new KeyVaultMeshSecretsPostConfigure(provider);
        var config = CreateConfig(mailboxMtlsPfxCertName: certName);

        sut.PostConfigure(null, config);

        Assert.Equal(expectedPfxBase64, config.MailboxMTLSPfx);
    }

    [Fact]
    public void ShouldThrowSanitisedException_WhenKeyVaultRequestFails()
    {
        const string certName = "MailboxMTLSPfx";
        const string sensitiveUpstreamDetail = "raw-upstream-diagnostic-payload";
        var secretClient = Substitute.For<SecretClient>();
        secretClient
            .GetSecret(certName)
            .Throws(new RequestFailedException(403, sensitiveUpstreamDetail));

        using var provider = new ServiceCollection()
            .AddSingleton(secretClient)
            .BuildServiceProvider();
        var sut = new KeyVaultMeshSecretsPostConfigure(provider);
        var config = CreateConfig(mailboxMtlsPfxCertName: certName);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            sut.PostConfigure(null, config)
        );

        Assert.Contains(certName, exception.Message);
        Assert.Contains("403", exception.Message);
        Assert.DoesNotContain(sensitiveUpstreamDetail, exception.Message);
        Assert.Null(exception.InnerException);
    }
}
