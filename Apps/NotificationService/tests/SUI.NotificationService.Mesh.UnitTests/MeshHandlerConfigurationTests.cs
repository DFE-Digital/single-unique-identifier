using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SUI.NotificationService.Mesh.Configuration;

namespace SUI.NotificationService.Mesh.UnitTests;

public sealed class MeshHandlerConfigurationTests : IDisposable
{
    private readonly TestCertificates _certificates = new();

    public void Dispose() => _certificates.Dispose();

    [Fact]
    public void ShouldPresentClientCertAndCustomTrust_WhenAcceptLocalDevCertFalse()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [$"{NhsMeshConfig.SectionName}:MailboxBaseUrl"] =
                        "https://msg.intspineservices.nhs.uk",
                    [$"{NhsMeshConfig.SectionName}:SharedKey"] = "shared-key",
                    [$"{NhsMeshConfig.SectionName}:MailboxId"] = "X26ABC1",
                    [$"{NhsMeshConfig.SectionName}:MailboxPassword"] = "password",
                    [$"{NhsMeshConfig.SectionName}:AcceptLocalDevCert"] = "false",
                    [$"{NhsMeshConfig.SectionName}:MailboxMTLSPfx"] = _certificates.PfxBase64(),
                    [$"{NhsMeshConfig.SectionName}:MailboxMTLSPfxPassword"] =
                        TestCertificates.PfxPassword,
                    [$"{NhsMeshConfig.SectionName}:MailboxRootCA"] = _certificates.RootPemBase64(),
                    [$"{NhsMeshConfig.SectionName}:MailboxIntermediateCA"] =
                        _certificates.IntermediatePemBase64(),
                }
            )
            .Build();

        using var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddMeshIntegration()
            .BuildServiceProvider();

        using var handler = serviceProvider
            .GetRequiredService<IHttpMessageHandlerFactory>()
            .CreateHandler(MeshInboxClient.HttpClientName);

        var sockets = FindPrimaryHandler(handler);
        var clientContext = sockets.SslOptions.ClientCertificateContext!;
        Assert.Equal(_certificates.Leaf.Thumbprint, clientContext.TargetCertificate.Thumbprint);
        Assert.Contains(
            clientContext.IntermediateCertificates,
            certificate => certificate.Thumbprint == _certificates.Intermediate.Thumbprint
        );

        var chainPolicy = sockets.SslOptions.CertificateChainPolicy!;
        Assert.Equal(X509ChainTrustMode.CustomRootTrust, chainPolicy.TrustMode);
        Assert.Equal(X509RevocationMode.Online, chainPolicy.RevocationMode);
        Assert.Equal(
            _certificates.Root.Thumbprint,
            Assert.Single(chainPolicy.CustomTrustStore).Thumbprint
        );
        Assert.Equal(
            _certificates.Intermediate.Thumbprint,
            Assert.Single(chainPolicy.ExtraStore).Thumbprint
        );
    }

    private static SocketsHttpHandler FindPrimaryHandler(HttpMessageHandler handler)
    {
        while (handler is DelegatingHandler delegating)
        {
            handler = delegating.InnerHandler!;
        }

        return Assert.IsType<SocketsHttpHandler>(handler);
    }
}
