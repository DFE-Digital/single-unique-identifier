using Microsoft.Extensions.DependencyInjection;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.FindApi.Startup;

public static class AzureTableServiceRegistration
{
    public static void AddAzureTableServices(this IServiceCollection services)
    {
        var interfaceType = typeof(ITableServiceEnsureCreated);
        var assembly = typeof(ITableServiceEnsureCreated).Assembly;

        foreach (
            var type in assembly
                .GetTypes()
                .Where(t =>
                    interfaceType.IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false }
                )
        )
        {
            services.AddTransient(interfaceType, type);
        }
    }
}
