using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SUI.Find.FindApi.Middleware;
using SUI.Find.FindApi.Startup;
using SUI.Find.Infrastructure;
using SUI.Find.Infrastructure.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder
    .Services.AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Middleware - Order is important
builder.UseMiddleware<AuthApiKeyMiddleware>(); // <--- Must ALWAYS be first
builder.UseMiddleware<AuditMiddleware>();

// Configuration
builder.Services.AddHealthChecks();
builder.Services.AddLogging();

// Register Services
builder.Services.AddSingleton<IAuditService, StorageTableAuditService>();
builder.Services.AddSingleton<IStorageTableAuditService, StorageTableAuditService>();
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connection = config.GetValue<string>("AzureWebJobsStorage");

    return new TableServiceClient(connection);
});

// Hosted Services
builder.Services.AddHostedService<AzureStorageTableStartup>();

await builder.Build().RunAsync();
