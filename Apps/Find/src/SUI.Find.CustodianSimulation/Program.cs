using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SUI.Find.CustodianSimulation.Interfaces;
using SUI.Find.CustodianSimulation.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder
    .Services.AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IDataProvider, FileDataProvider>();
builder.Services.AddSingleton<IRandomDelayService>(_ => new RandomDelayService(3, 10));

builder.UseMiddleware<ScopeEnforcementService>();

builder.Build().Run();
