using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using SUI.Find.Application.Interfaces;

namespace SUI.Find.FindApi.Startup;

[ExcludeFromCodeCoverage(Justification = "Hosted service startup code.")]
public class AzureStorageTableStartup(ITableStorageAuditService auditService) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await auditService.EnsureAuditTableExistsAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
