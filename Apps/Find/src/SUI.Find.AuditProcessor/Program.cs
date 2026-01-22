using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SUI.Find.Application.Interfaces;
using SUI.Find.AuditProcessor.Startup;
using SUI.Find.Infrastructure;
using SUI.Find.Infrastructure.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder
    .Services.AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddSingleton<IAuditService, AuditStorageTableService>();
builder.Services.AddAzureTableServices();

// Add this after other service registrations
builder.Services.AddHostedService<AzureStorageTableStartup>();
builder.Services.AddHostedService<AzureStorageQueueStartup>();

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connection = config.GetValue<string>("AzureWebJobsStorage");

    return new TableServiceClient(connection);
});

await builder.Build().RunAsync();
