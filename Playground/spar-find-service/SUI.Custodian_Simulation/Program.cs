using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Interfaces;
using Functions;

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
        services.AddSingleton<IDataProvider, FileDataProvider>();
        services.AddSingleton<IRandomDelayService>(_ => new RandomDelayService(3, 10));
    })
    .Build();

host.Run();
