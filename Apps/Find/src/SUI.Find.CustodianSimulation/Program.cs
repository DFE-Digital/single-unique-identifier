using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SUI.Find.CustodianSimulation.Interfaces;
using SUI.Find.CustodianSimulation.Services;
using SUI.Find.Infrastructure.Extensions;

var builder = FunctionsApplication.CreateBuilder(args);

builder.UseOpenTelemetry();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IDataProvider, FileDataProvider>();
builder.Services.AddSingleton<IRandomDelayService>(_ => new RandomDelayService(3, 10));

builder.UseMiddleware<ScopeEnforcementService>();

builder.Build().Run();
