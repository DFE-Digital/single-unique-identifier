using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SUI.GetAnIdentifier.Infrastructure;

namespace SUI.GetAnIdentifier.API.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStartupConfigurationValidation(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<AuthSettings>()
            .Bind(configuration.GetSection(AuthSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<AuthTokenServiceConfig>()
            .Bind(configuration.GetSection(AuthTokenServiceConfig.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<GetAnIdentifierConfiguration>()
            .Bind(configuration.GetSection(GetAnIdentifierConfiguration.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register the custom PEM validator
        services.AddSingleton<
            IValidateOptions<AuthTokenServiceConfig>,
            AuthTokenServiceConfigValidator
        >();

        return services;
    }
}
