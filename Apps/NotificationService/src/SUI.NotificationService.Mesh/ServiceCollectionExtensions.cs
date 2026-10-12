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

        meshHttpClient
        // Local sandbox only: no client certificate is presented, so connections to a real MESH
        // environment (INT/LIVE), which requires mutual TLS with an NHS-issued certificate, will
        // fail the TLS handshake. Client certificate support is deferred until a deployed
        // environment exists - see the README's NHS MESH section.
        .ConfigurePrimaryHttpMessageHandler(
            static (handler, serviceProvider) =>
            {
                var config = serviceProvider.GetRequiredService<IOptions<NhsMeshConfig>>().Value;

                if (config.AcceptLocalDevCert)
                {
                    if (handler is not SocketsHttpHandler socketsHandler)
                    {
                        throw new InvalidOperationException(
                            $"Expected {nameof(SocketsHttpHandler)} as the MESH primary handler but got {handler.GetType().Name}."
                        );
                    }

                    // The local MESH sandbox (see compose.yaml) presents a self-signed certificate.
                    // Options validation restricts this to a loopback MailboxBaseUrl, so real MESH
                    // traffic always has its server certificate verified.
                    socketsHandler.SslOptions.RemoteCertificateValidationCallback = static (
                        _,
                        _,
                        _,
                        _
                    ) => true;
                }
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

    private static bool IsHttps(string mailboxBaseUrl) =>
        Uri.TryCreate(mailboxBaseUrl, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps;

    private static bool IsLoopback(string mailboxBaseUrl) =>
        Uri.TryCreate(mailboxBaseUrl, UriKind.Absolute, out var uri) && uri.IsLoopback;
}
