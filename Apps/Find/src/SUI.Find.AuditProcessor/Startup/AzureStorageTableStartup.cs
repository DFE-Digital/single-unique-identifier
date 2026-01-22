using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using SUI.Find.Infrastructure.Interfaces;

namespace SUI.Find.AuditProcessor.Startup;

[ExcludeFromCodeCoverage(Justification = "Hosted service startup code.")]
public class AzureStorageTableStartup(IEnumerable<ITableServiceEnsureCreated> tableServices)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var tableService in tableServices)
        {
            await tableService.EnsureAuditTableExistsAsync(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
