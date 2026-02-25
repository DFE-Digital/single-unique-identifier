using System.Diagnostics.CodeAnalysis;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SUI.Find.Application.Interfaces;
using SUI.Find.Application.Interfaces.Matching;
using SUI.Find.Infrastructure.Clients;
using SUI.Find.Infrastructure.Configuration;
using SUI.Find.Infrastructure.Factories;
using SUI.Find.Infrastructure.Factories.Fhir;
using SUI.Find.Infrastructure.Interfaces;
using SUI.Find.Infrastructure.Interfaces.Fhir;
using SUI.Find.Infrastructure.Repositories.SearchResultEntryStorage;
using SUI.Find.Infrastructure.Repositories.SuiCustodianRegister;
using SUI.Find.Infrastructure.Services;
using SUI.Find.Infrastructure.Services.Fhir;
using SUI.Find.Infrastructure.Utility;

namespace SUI.Find.Infrastructure.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddSingleton<IAuditService, AuditStorageTableService>();
        services.AddSingleton<IAuditQueueClient, AuditQueueClient>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IQueueClientFactory, QueueClientFactory>();
        services.AddSingleton<IHashService, HashService>();
        services.AddSingleton<IProviderHttpClient, ProviderHttpClient>();
        services.AddSingleton<IFetchUrlStorageService, UrlStorageTableService>();
        services.AddSingleton<IPersonIdEncryptionService, PersonIdEncryptionService>();
        services.AddSingleton<IBuildCustodianRequestService, BuildCustodianRequestsService>();
        services.AddSingleton<IBuildCustodianHttpRequest, BuildCustodianHttpRequest>();
        services.AddSingleton<IIdRegisterRepository, SuiCustodianRegisterRepository>();
        services.AddSingleton<ISearchResultEntryRepository, SearchResultEntryRepository>();
        services.AddSingleton<IFhirClientFactory, FhirClientFactory>();
        services.AddSingleton<IFhirService, FhirService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<ISecretService, AzureKeyVaultSecretService>();

        var useStubAuthTokenService = configuration.GetValue<bool>("UseStubAuthTokenService");
        if (useStubAuthTokenService)
        {
            services.AddSingleton<IFhirAuthTokenService, StubFhirAuthTokenService>();
        }
        else
        {
            services.AddSingleton<IFhirAuthTokenService, FhirAuthTokenService>();
        }

        services.AddAzureTableServices();

        return services;
    }

    public static IServiceCollection AddSecretClientServices(this IServiceCollection services)
    {
        services
            .AddOptions<AzureSecretConfiguration>()
            .BindConfiguration(AzureSecretConfiguration.SectionName);

        services.AddSingleton<SecretClient>(sp =>
        {
            var config = sp.GetRequiredService<IOptions<AzureSecretConfiguration>>().Value;
            var uriString =
                config.KeyVaultUri
                ?? "https://localdevtotallyrandomaddressbecauseitdoesntrunwiththestubauthtokenservice";
            return new SecretClient(
                vaultUri: new Uri(uriString),
                credential: new DefaultAzureCredential()
            );
        });

        return services;
    }
}
