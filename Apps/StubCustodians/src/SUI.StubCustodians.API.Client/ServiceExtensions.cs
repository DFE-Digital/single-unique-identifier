using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace SUI.StubCustodians.API.Client;

[ExcludeFromCodeCoverage]
public static class ServiceExtensions
{
    public static IServiceCollection AddTransferClient(
        this IServiceCollection services,
        string baseAddress,
        string apiKey
    )
    {
        services.AddHttpClient(
            nameof(StubCustodiansApi),
            (sp, client) =>
            {
                client.BaseAddress = new Uri(baseAddress);
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            }
        );

        services.AddTransient<IStubCustodiansApi, StubCustodiansApi>(
            provider => new StubCustodiansApi(
                provider
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(nameof(StubCustodiansApi))
            )
        );
        return services;
    }
}
