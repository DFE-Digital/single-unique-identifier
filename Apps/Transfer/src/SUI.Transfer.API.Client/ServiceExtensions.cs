using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace SUI.Transfer.API.Client;

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
            nameof(TransferApi),
            (sp, client) =>
            {
                client.BaseAddress = new Uri(baseAddress);
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            }
        );

        services.AddTransient<ITransferApi, TransferApi>(provider => new TransferApi(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(TransferApi))
        ));
        return services;
    }
}
