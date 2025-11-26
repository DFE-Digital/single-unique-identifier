namespace SUI.Find.Application.Interfaces;

public interface ITableStorageAuditService : IAuditService
{
    Task EnsureAuditTableExistsAsync(CancellationToken cancellationToken);
}
