using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SUI.Find.Application.Constants;
using SUI.Find.Infrastructure.Factories;

namespace SUI.Find.FindApi.Startup;

[ExcludeFromCodeCoverage(Justification = "Hosted service startup code.")]
public class AzureStorageQueueStartup(IQueueClientFactory queueClientFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken) =>
        await queueClientFactory.CreateQueuesIfNotExistsAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
