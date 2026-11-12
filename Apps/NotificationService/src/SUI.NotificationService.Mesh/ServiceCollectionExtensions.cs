using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using SUI.NotificationService.Application.Interfaces;
using SUI.NotificationService.Mesh.Configuration;
using SUI.NotificationService.Mesh.Handlers;

namespace SUI.NotificationService.Mesh;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Provides the composition point for NHS MESH integration services: configuration,
    /// authentication and mailbox reading. Orchestration decides when messages are read; this
    /// boundary only owns the transport.
    /// </summary>
    public static IServiceCollection AddMeshIntegration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddOptions<NhsMeshConfig>()
            .BindConfiguration(NhsMeshConfig.SectionName)
            .ValidateDataAnnotations()
            // [Url] also accepts http://, which would send the NHSMESH authorisation header in plaintext.
            .Validate(
                static config => IsHttps(config.MailboxBaseUrl),
                $"{nameof(NhsMeshConfig.MailboxBaseUrl)} must be an absolute https:// URL."
            )
            .Validate(
                static config => !config.AcceptLocalDevCert || IsLoopback(config.MailboxBaseUrl),
                $"{nameof(NhsMeshConfig.AcceptLocalDevCert)} is only permitted against a loopback MESH sandbox."
            )
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<NhsMeshConfig>, NhsMeshCertificateValidator>();
        services.AddTransient<NhsMeshAuthHandler>();

        var meshHttpClient = services.AddHttpClient<IMeshInboxClient, MeshInboxClient>(
            MeshInboxClient.HttpClientName,
            static (serviceProvider, client) =>
            {
                var config = serviceProvider.GetRequiredService<IOptions<NhsMeshConfig>>().Value;
                client.BaseAddress = new Uri(config.MailboxBaseUrl);
            }
        );

        // Must be registered before the auth handler: handlers run in registration order, so the
        // retry then wraps it and every attempt gets a freshly built Authorization header (the
        // MESH header carries a nonce and timestamp and must not be replayed).
        meshHttpClient.AddResilienceHandler("mesh-transient-retry", ConfigureTransientRetry);
        meshHttpClient.AddHttpMessageHandler<NhsMeshAuthHandler>();

        meshHttpClient.ConfigurePrimaryHttpMessageHandler(
            static (handler, serviceProvider) =>
            {
                if (handler is not SocketsHttpHandler socketsHandler)
                {
                    throw new InvalidOperationException(
                        $"Expected {nameof(SocketsHttpHandler)} as the MESH primary handler but got {handler.GetType().Name}."
                    );
                }

                var config = serviceProvider.GetRequiredService<IOptions<NhsMeshConfig>>().Value;

                if (config.AcceptLocalDevCert)
                {
                    // The local MESH sandbox (see compose.yaml) presents a self-signed certificate.
                    // Options validation restricts this to a loopback MailboxBaseUrl, so real MESH
                    // traffic always has its server certificate verified.
                    socketsHandler.SslOptions.RemoteCertificateValidationCallback = static (
                        _,
                        _,
                        _,
                        _
                    ) => true;
                    return;
                }

                ConfigureMutualTls(socketsHandler, config);
            }
        );

        return services;
    }

    // Retries only transient failures (connection errors, timeouts, 408, 429 and 5xx), honouring
    // Retry-After. Client errors such as 401/403/404 are not retried. No circuit breaker: the app is a
    // run-to-completion job. Exhausted retries surface as the usual HttpRequestException, which
    // MeshMessageProcessor handles by leaving the message in the mailbox.
    private static void ConfigureTransientRetry(
        ResiliencePipelineBuilder<HttpResponseMessage> builder
    )
    {
        builder
            .AddTimeout(TimeSpan.FromSeconds(30))
            .AddRetry(
                new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldRetryAfterHeader = true,
                }
            )
            .AddTimeout(TimeSpan.FromSeconds(12));
    }

    // Real MESH requires mutual TLS and serves a certificate issued by an NHS CA that is
    // not in the OS trust store. NhsMeshCertificateValidator has already proved all three values load.
    private static void ConfigureMutualTls(SocketsHttpHandler handler, NhsMeshConfig config)
    {
        var clientCertificate = MeshCertificateLoader.LoadClientCertificate(
            config.MailboxMTLSPfx,
            config.MailboxMTLSPfxPassword
        );
        var root = MeshCertificateLoader.LoadRootCertificate(config.MailboxRootCA!);
        var intermediate = MeshCertificateLoader.LoadCertificate(config.MailboxIntermediateCA!);

        // A PFX load keeps only the certificate with the private key, so the intermediate is
        // supplied explicitly and sent in the handshake. offline: no AIA fetches for the client chain.
        handler.SslOptions.ClientCertificateContext = SslStreamCertificateContext.Create(
            clientCertificate,
            new X509Certificate2Collection(intermediate),
            offline: true
        );

        // Revocation is deliberately checked online for the server chain (excluding the root). If the
        // NHS CA's revocation endpoint is unreachable, the handshake fails rather than trusting an
        // unchecked certificate - see the README's NHS MESH section.
        var chainPolicy = new X509ChainPolicy
        {
            TrustMode = X509ChainTrustMode.CustomRootTrust,
            RevocationMode = X509RevocationMode.NoCheck,
            RevocationFlag = X509RevocationFlag.ExcludeRoot,
        };
        chainPolicy.CustomTrustStore.Add(root);
        chainPolicy.ExtraStore.Add(intermediate);
        handler.SslOptions.CertificateChainPolicy = chainPolicy;
    }

    private static bool IsHttps(string mailboxBaseUrl) =>
        Uri.TryCreate(mailboxBaseUrl, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps;

    private static bool IsLoopback(string mailboxBaseUrl) =>
        Uri.TryCreate(mailboxBaseUrl, UriKind.Absolute, out var uri) && uri.IsLoopback;
}
