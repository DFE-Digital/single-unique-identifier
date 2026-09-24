using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SUI.NotificationService.Mesh.Configuration;

namespace SUI.NotificationService.Mesh.UnitTests.Configuration;

public sealed class NhsMeshConfigValidationTests
{
    [Theory]
    [InlineData("https://localhost:8700")]
    [InlineData("https://127.0.0.1:8700")]
    [InlineData("https://[::1]:8700")]
    public void AcceptLocalDevCert_IsPermitted_ForLoopbackMailboxBaseUrl(string mailboxBaseUrl)
    {
        var config = ResolveConfig(mailboxBaseUrl, acceptLocalDevCert: true);

        Assert.True(config.AcceptLocalDevCert);
    }

    [Theory]
    [InlineData("https://msg.intspineservices.nhs.uk")]
    [InlineData("https://mesh-sandbox:8700")]
    public void AcceptLocalDevCert_IsRejected_ForNonLoopbackMailboxBaseUrl(string mailboxBaseUrl)
    {
        var exception = Assert.Throws<OptionsValidationException>(() =>
            ResolveConfig(mailboxBaseUrl, acceptLocalDevCert: true)
        );

        Assert.Contains(
            exception.Failures,
            failure => failure.Contains(nameof(NhsMeshConfig.AcceptLocalDevCert))
        );
    }

    [Fact]
    public void AcceptLocalDevCert_IsRejected_WhenMailboxBaseUrlIsNotAnAbsoluteUrl()
    {
        var exception = Assert.Throws<OptionsValidationException>(() =>
            ResolveConfig("not-a-url", acceptLocalDevCert: true)
        );

        Assert.Contains(
            exception.Failures,
            failure => failure.Contains(nameof(NhsMeshConfig.AcceptLocalDevCert))
        );
    }

    [Fact]
    public void NonLoopbackMailboxBaseUrl_IsPermitted_WhenAcceptLocalDevCertIsFalse()
    {
        var config = ResolveConfig(
            "https://msg.intspineservices.nhs.uk",
            acceptLocalDevCert: false
        );

        Assert.False(config.AcceptLocalDevCert);
    }

    private static NhsMeshConfig ResolveConfig(string mailboxBaseUrl, bool acceptLocalDevCert)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [$"{NhsMeshConfig.SectionName}:MailboxBaseUrl"] = mailboxBaseUrl,
                    [$"{NhsMeshConfig.SectionName}:SharedKey"] = "shared-key",
                    [$"{NhsMeshConfig.SectionName}:MailboxId"] = "X26ABC1",
                    [$"{NhsMeshConfig.SectionName}:MailboxPassword"] = "password",
                    [$"{NhsMeshConfig.SectionName}:AcceptLocalDevCert"] =
                        acceptLocalDevCert.ToString(),
                }
            )
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddMeshIntegration();

        using var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IOptions<NhsMeshConfig>>().Value;
    }
}
