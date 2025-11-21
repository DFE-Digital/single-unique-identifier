using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SUI.Find.FindApi;
using SUI.Find.FindApi.OpenApi;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder
    .Services.AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddHealthChecks();
builder.Services.AddLogging();

builder.Services.AddSingleton<IOpenApiConfigurationOptions, CustomOpenApiConfigurationOptions>();

builder.UseMiddleware<AuthApiKeyMiddleware>();

await builder.Build().RunAsync();
