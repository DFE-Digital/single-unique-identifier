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
    public static IServiceCollection AddMeshIntegration(
        this IServiceCollection services,
        IHostEnvironment environment
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(environment);

        services
            .AddOptions<NhsMeshConfig>()
            .BindConfiguration(NhsMeshConfig.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddTransient<NhsMeshAuthHandler>();

        var meshHttpClientBuilder = services
            .AddHttpClient<IMeshMessageReceiver, MeshInboxClient>(
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

        if (environment.IsDevelopment())
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
