using System.IO.Abstractions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SUI.Find.Infrastructure.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder
    .Services.AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Third-party and framework services
builder.Services.AddHealthChecks();
builder.Services.AddLogging();
builder.Services.AddSingleton<IFileSystem, FileSystem>();

// Custom application services
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IAuthStoreService, FileAuthStoreService>();
}
else
{
    builder.Services.AddSingleton<IAuthStoreService, SecureAuthStoreService>();
}

await builder.Build().RunAsync();
