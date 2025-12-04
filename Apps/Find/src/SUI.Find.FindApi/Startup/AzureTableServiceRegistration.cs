using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using SUI.Find.Infrastructure.Interfaces;

namespace SUI.Find.FindApi.Startup;

[ExcludeFromCodeCoverage(Justification = "Simple DI registration code")]
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
