using Microsoft.Extensions.Hosting;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.FindApi.Startup;

public class AzureStorageTableStartup(IStorageTableAuditService auditService) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await auditService.EnsureAuditTableExistsAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
