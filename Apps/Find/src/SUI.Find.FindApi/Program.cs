using System.IO.Abstractions;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SUI.Find.Application.Interfaces;
using SUI.Find.FindApi.Factories;
using SUI.Find.FindApi.Middleware;
using SUI.Find.FindApi.Startup;
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
builder.Services.AddSingleton<IAuditService, AuditStorageTableService>();
builder.Services.AddSingleton<ITableStorageAuditService, AuditStorageTableService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IPersonIdEncryptionService, PersonIdEncryptionService>();
builder.Services.AddSingleton<IQueueClientFactory, QueueClientFactory>();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IAuthStoreService, FileAuthStoreService>();
}
else
{
    builder.Services.AddSingleton<IAuthStoreService, SecureAuthStoreService>();
}

// Add this after other service registrations
builder.Services.AddHostedService<AzureStorageTableStartup>();
builder.Services.AddHostedService<AzureStorageQueueStartup>();

builder.UseMiddleware<JwtAuthMiddleware>();
builder.UseMiddleware<AuditMiddleware>();

builder.Services.AddSingleton<QueueClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new QueueClient(config["AzureWebJobsStorage"], "audit-queue");
});

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connection = config.GetValue<string>("AzureWebJobsStorage");

    return new TableServiceClient(connection);
});

await builder.Build().RunAsync();
