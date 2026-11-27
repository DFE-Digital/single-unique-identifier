using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Hosting;
using SUI.NotificationService.Application;
using SUI.NotificationService.Infrastructure;
using SUI.NotificationService.Mesh;
using SUI.NotificationService.Webhooks;

namespace SUI.NotificationService;

internal static class NotificationServiceHost
{
    public static IHost Build(string[] args)
    {
        var settings = new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory,
        };
        var builder = Host.CreateApplicationBuilder(settings);

        TokenCredential credential = new DefaultAzureCredential();

        builder.Configuration.AddMeshKeyVaultConfiguration(credential);

        builder.Services.AddNotificationServiceApplication();
        builder.Services.AddMeshIntegration();
        builder.Services.AddSupplierWebhookDelivery();
        builder.Services.AddNotificationServiceInfrastructure(builder.Configuration, credential);

        return builder.Build();
    }
}
