using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SUI.NotificationService.Mesh.Configuration;

namespace SUI.NotificationService.Mesh.UnitTests.Configuration;

public sealed class NhsMeshConfigValidationTests
{
    [Theory]
    [InlineData("https://localhost:8700")]
    [InlineData("https://127.0.0.1:8700")]
    [InlineData("https://[::1]:8700")]
    public async Task AcceptLocalDevCert_IsPermitted_ForLoopbackMailboxBaseUrl(
        string mailboxBaseUrl
    )
    {
        var config = await StartHostAndResolveConfigAsync(mailboxBaseUrl, acceptLocalDevCert: true);

        Assert.True(config.AcceptLocalDevCert);
    }

    [Theory]
    [InlineData("https://msg.intspineservices.nhs.uk")]
    [InlineData("https://mesh-sandbox:8700")]
    public async Task AcceptLocalDevCert_IsRejected_ForNonLoopbackMailboxBaseUrl(
        string mailboxBaseUrl
    )
    {
        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() =>
            StartHostAndResolveConfigAsync(mailboxBaseUrl, acceptLocalDevCert: true)
        );

        Assert.Contains(
            exception.Failures,
            failure => failure.Contains(nameof(NhsMeshConfig.AcceptLocalDevCert))
        );
    }

    [Fact]
    public async Task AcceptLocalDevCert_IsRejected_WhenMailboxBaseUrlIsNotAnAbsoluteUrl()
    {
        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() =>
            StartHostAndResolveConfigAsync("not-a-url", acceptLocalDevCert: true)
        );

        Assert.Contains(
            exception.Failures,
            failure => failure.Contains(nameof(NhsMeshConfig.AcceptLocalDevCert))
        );
    }

    [Fact]
    public async Task NonLoopbackMailboxBaseUrl_IsPermitted_WhenAcceptLocalDevCertIsFalse()
    {
        var config = await StartHostAndResolveConfigAsync(
            "https://msg.intspineservices.nhs.uk",
            acceptLocalDevCert: false
        );

        Assert.False(config.AcceptLocalDevCert);
    }

    [Theory]
    [InlineData("http://msg.intspineservices.nhs.uk")]
    [InlineData("http://localhost:8700")]
    public async Task HttpMailboxBaseUrl_IsRejected(string mailboxBaseUrl)
    {
        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() =>
            StartHostAndResolveConfigAsync(mailboxBaseUrl, acceptLocalDevCert: false)
        );

        Assert.Contains(
            exception.Failures,
            failure => failure.Contains(nameof(NhsMeshConfig.MailboxBaseUrl))
        );
    }

    private static async Task<NhsMeshConfig> StartHostAndResolveConfigAsync(
        string mailboxBaseUrl,
        bool acceptLocalDevCert
    )
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

        using var host = new HostBuilder()
            .ConfigureAppConfiguration(builder => builder.AddConfiguration(configuration))
            .ConfigureServices((context, services) => services.AddMeshIntegration())
            .Build();

        // ValidateOnStart() validators run when the host starts, not when IOptions is resolved.
        await host.StartAsync();
        try
        {
            return host.Services.GetRequiredService<IOptions<NhsMeshConfig>>().Value;
        }
        finally
        {
            await host.StopAsync();
        }
    }
}
