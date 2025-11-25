using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Interfaces;
using Services;
using SUI.Find.Functions.Interfaces;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(worker =>
    {
        worker.UseMiddleware<ScopeEnforcementService>();
    })
    .ConfigureAppConfiguration(c =>
    {
        c.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
         .AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddHttpClient();
        services.AddHttpClient("providers");

        services.AddSingleton<ICustodianRegistry, CustodianRegistryService>();
        services.AddSingleton<IPepService, PepService>();

        services.AddSingleton<IOutboundAuthTokenService, OutboundAuthTokenService>();

        services.AddSingleton<ICoreFindService, CoreFindService>();
        services.AddSingleton<IFindOrchestrationService, FindOrchestrationService>();
        services.AddSingleton<IPersonIdEncryptionService, PersonIdEncryptionService>();
        services.AddSingleton<IPersonMatchService, PDSSimulationPersonMatchService>();
        services.AddSingleton<ICallerEncryptionResolver, CallerEncryptionResolver>();


        services.AddSingleton(new TableServiceClient("UseDevelopmentStorage=true"));
        services.AddSingleton<IFetchUrlMappingStore, FetchUrlMappingStore>();

    })
    .Build();

host.Run();
