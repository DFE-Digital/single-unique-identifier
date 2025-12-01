using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace SUI.Custodians.API.Client;

[ExcludeFromCodeCoverage]
public static class ServiceExtensions
{
    public static IServiceCollection AddCustodiansClient(
        this IServiceCollection services,
        string baseAddress,
        string apiKey
    )
    {
        services.AddHttpClient(
            nameof(CustodiansApi),
            (sp, client) =>
            {
                client.BaseAddress = new Uri(baseAddress);
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            }
        );

        services.AddTransient<ICustodiansApi, CustodiansApi>(provider => new CustodiansApi(
                provider
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(nameof(CustodiansApi))
            )
        );
        return services;
    }
}