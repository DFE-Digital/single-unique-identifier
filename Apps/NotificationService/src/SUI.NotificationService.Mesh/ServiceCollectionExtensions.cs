using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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

        services
            .AddHttpClient<IMeshInboxClient, MeshInboxClient>(
                MeshInboxClient.HttpClientName,
                static (serviceProvider, client) =>
                {
                    var config = serviceProvider
                        .GetRequiredService<IOptions<NhsMeshConfig>>()
                        .Value;
                    client.BaseAddress = new Uri(config.MailboxBaseUrl);
                }
            )
            .AddHttpMessageHandler<NhsMeshAuthHandler>()
            // Local sandbox only: no client certificate is presented, so connections to a real MESH
            // environment (INT/LIVE), which requires mutual TLS with an NHS-issued certificate, will
            // fail the TLS handshake. Client certificate support is deferred until a deployed
            // environment exists - see the README's NHS MESH section.
            .ConfigurePrimaryHttpMessageHandler(
                static (handler, serviceProvider) =>
                {
                    var config = serviceProvider
                        .GetRequiredService<IOptions<NhsMeshConfig>>()
                        .Value;

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

    private static bool IsHttps(string mailboxBaseUrl) =>
        Uri.TryCreate(mailboxBaseUrl, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps;

    private static bool IsLoopback(string mailboxBaseUrl) =>
        Uri.TryCreate(mailboxBaseUrl, UriKind.Absolute, out var uri) && uri.IsLoopback;
}
