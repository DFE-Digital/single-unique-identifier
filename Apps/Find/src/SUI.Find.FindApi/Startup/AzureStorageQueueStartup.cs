using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace SUI.Find.FindApi.Startup;

[ExcludeFromCodeCoverage(Justification = "Hosted service startup code.")]
public class AzureStorageQueueStartup : IHostedService
{
    private readonly IConfiguration _configuration;

    public AzureStorageQueueStartup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var connectionString = _configuration["AzureWebJobsStorage"];
        var queueClient = new QueueClient(connectionString, "audit-queue");
        await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
