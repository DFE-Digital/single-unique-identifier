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
            .ValidateOnStart();

        services.AddTransient<NhsMeshAuthHandler>();

        var meshHttpClientBuilder = services
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
            .AddHttpMessageHandler<NhsMeshAuthHandler>();

        // Get the NhsMeshConfig from the service provider to check if we are in development environment
        var serviceProvider = services.BuildServiceProvider();
        var config = serviceProvider.GetRequiredService<IOptions<NhsMeshConfig>>().Value;
        var acceptLocalDevCert = config.AcceptLocalDevCert;

        if (acceptLocalDevCert)
        {
            // The local MESH sandbox (see compose.yaml) presents a self-signed certificate.
            meshHttpClientBuilder.ConfigurePrimaryHttpMessageHandler(() =>
                new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                }
            );
        }

        return services;
    }
}
