using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SUI.NotificationService.Application;
using SUI.NotificationService.Infrastructure;
using SUI.NotificationService.Mns;
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

        builder.Services.AddNotificationServiceApplication();
        builder.Services.AddMnsIntegration();
        builder.Services.AddSupplierWebhookDelivery();
        builder.Services.AddNotificationServiceInfrastructure();

        return builder.Build();
    }
}
