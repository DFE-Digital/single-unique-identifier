using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SUI.NotificationService.Mesh.Configuration;

namespace SUI.NotificationService.Mesh.UnitTests.Configuration;

public sealed class NhsMeshConfigValidationTests : IDisposable
{
    private const string IntMailboxBaseUrl = "https://msg.intspineservices.nhs.uk";
    private readonly TestCertificates _certificates = new();

    public void Dispose() => _certificates.Dispose();

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
            IntMailboxBaseUrl,
            acceptLocalDevCert: false,
            ValidCertificateSettings()
        );

        Assert.False(config.AcceptLocalDevCert);
    }

    [Fact]
    public async Task ShouldFailStartup_WhenAcceptLocalDevCertFalseAndPfxMissing()
    {
        var settings = ValidCertificateSettings();
        settings.Remove(Key(nameof(NhsMeshConfig.MailboxMTLSPfx)));

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() =>
            StartHostAndResolveConfigAsync(IntMailboxBaseUrl, acceptLocalDevCert: false, settings)
        );

        var failure = Assert.Single(exception.Failures);
        Assert.Contains(nameof(NhsMeshConfig.MailboxMTLSPfx), failure);
    }

    [Theory]
    [InlineData(nameof(NhsMeshConfig.MailboxRootCA))]
    [InlineData(nameof(NhsMeshConfig.MailboxIntermediateCA))]
    public async Task ShouldFailStartup_WhenAcceptLocalDevCertFalseAndCaCertificateMissing(
        string key
    )
    {
        var settings = ValidCertificateSettings();
        settings[Key(key)] = "";

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() =>
            StartHostAndResolveConfigAsync(IntMailboxBaseUrl, acceptLocalDevCert: false, settings)
        );

        var failure = Assert.Single(exception.Failures);
        Assert.Contains(key, failure);
    }

    [Fact]
    public async Task ShouldFailStartupNamingEachKey_WhenAcceptLocalDevCertFalseAndBothCertsMissing()
    {
        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() =>
            StartHostAndResolveConfigAsync(IntMailboxBaseUrl, acceptLocalDevCert: false)
        );

        Assert.Contains(
            exception.Failures,
            failure => failure.Contains(nameof(NhsMeshConfig.MailboxMTLSPfx))
        );
        Assert.Contains(
            exception.Failures,
            failure => failure.Contains(nameof(NhsMeshConfig.MailboxRootCA))
        );
        Assert.Contains(
            exception.Failures,
            failure => failure.Contains(nameof(NhsMeshConfig.MailboxIntermediateCA))
        );
    }

    [Fact]
    public async Task ShouldFailStartup_WhenPfxPasswordIsWrong()
    {
        const string wrongPassword = "wrong-pfx-password";
        var settings = ValidCertificateSettings();
        settings[Key(nameof(NhsMeshConfig.MailboxMTLSPfxPassword))] = wrongPassword;

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() =>
            StartHostAndResolveConfigAsync(IntMailboxBaseUrl, acceptLocalDevCert: false, settings)
        );

        var failure = Assert.Single(exception.Failures);
        Assert.Contains(nameof(NhsMeshConfig.MailboxMTLSPfx), failure);
        Assert.DoesNotContain(wrongPassword, exception.Message);
        Assert.DoesNotContain(TestCertificates.PfxPassword, exception.Message);
        Assert.DoesNotContain(
            settings[Key(nameof(NhsMeshConfig.MailboxMTLSPfx))]!,
            exception.Message
        );
    }

    [Theory]
    [InlineData(nameof(NhsMeshConfig.MailboxRootCA), "not base64!")]
    [InlineData(nameof(NhsMeshConfig.MailboxRootCA), "bm90IGEgY2VydA==")] // base64 of "not a cert"
    [InlineData(nameof(NhsMeshConfig.MailboxIntermediateCA), "not base64!")]
    [InlineData(nameof(NhsMeshConfig.MailboxIntermediateCA), "bm90IGEgY2VydA==")]
    public async Task ShouldFailStartup_WhenCaCertificateIsInvalid(string key, string value)
    {
        var settings = ValidCertificateSettings();
        settings[Key(key)] = value;

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() =>
            StartHostAndResolveConfigAsync(IntMailboxBaseUrl, acceptLocalDevCert: false, settings)
        );

        var failure = Assert.Single(exception.Failures);
        Assert.Contains(key, failure);
        Assert.DoesNotContain(value, exception.Message);
    }

    [Fact]
    public async Task ShouldFailStartup_WhenRootCaIsNotSelfSigned()
    {
        var settings = ValidCertificateSettings();
        settings[Key(nameof(NhsMeshConfig.MailboxRootCA))] = _certificates.IntermediatePemBase64();

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() =>
            StartHostAndResolveConfigAsync(IntMailboxBaseUrl, acceptLocalDevCert: false, settings)
        );

        Assert.Contains(nameof(NhsMeshConfig.MailboxRootCA), Assert.Single(exception.Failures));
    }

    [Fact]
    public async Task ShouldFailStartup_WhenIntermediateIsNotIssuedByRoot()
    {
        using var other = new TestCertificates();
        var settings = ValidCertificateSettings();
        settings[Key(nameof(NhsMeshConfig.MailboxRootCA))] = other.RootPemBase64();

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() =>
            StartHostAndResolveConfigAsync(IntMailboxBaseUrl, acceptLocalDevCert: false, settings)
        );

        Assert.Contains(
            nameof(NhsMeshConfig.MailboxIntermediateCA),
            Assert.Single(exception.Failures)
        );
    }

    [Fact]
    public async Task ShouldStart_WhenCaCertificatesAreDer()
    {
        var settings = ValidCertificateSettings();
        settings[Key(nameof(NhsMeshConfig.MailboxRootCA))] = TestCertificates.DerBase64(
            _certificates.Root
        );
        settings[Key(nameof(NhsMeshConfig.MailboxIntermediateCA))] = TestCertificates.DerBase64(
            _certificates.Intermediate
        );

        var config = await StartHostAndResolveConfigAsync(
            IntMailboxBaseUrl,
            acceptLocalDevCert: false,
            settings
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
        bool acceptLocalDevCert,
        Dictionary<string, string?>? extraSettings = null
    )
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(extraSettings ?? [])
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

    private Dictionary<string, string?> ValidCertificateSettings() =>
        new()
        {
            [Key(nameof(NhsMeshConfig.MailboxMTLSPfx))] = _certificates.PfxBase64(),
            [Key(nameof(NhsMeshConfig.MailboxMTLSPfxPassword))] = TestCertificates.PfxPassword,
            [Key(nameof(NhsMeshConfig.MailboxRootCA))] = _certificates.RootPemBase64(),
            [Key(nameof(NhsMeshConfig.MailboxIntermediateCA))] =
                _certificates.IntermediatePemBase64(),
        };

    private static string Key(string name) => $"{NhsMeshConfig.SectionName}:{name}";
}
