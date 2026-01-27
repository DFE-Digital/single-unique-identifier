using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using SUI.Find.Application.Factories.PdsSearch;
using SUI.Find.Application.Interfaces;

namespace SUI.Find.Application.Extensions;

[ExcludeFromCodeCoverage(Justification = "Extension methods for DI container")]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPdsSearchStrategies(this IServiceCollection services)
    {
        var strategyType = typeof(IPdsSearchStrategy);
        var strategies = strategyType
            .Assembly.GetTypes()
            .Where(t => strategyType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var strategy in strategies)
        {
            services.AddSingleton(strategyType, strategy);
        }

        services.AddSingleton<IPdsSearchFactory, PdsSearchFactory>();

        return services;
    }
}
