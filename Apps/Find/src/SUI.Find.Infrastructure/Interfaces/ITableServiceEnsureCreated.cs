namespace SUI.Find.Infrastructure.Interfaces;

public interface ITableServiceEnsureCreated
{
    Task EnsureAuditTableExistsAsync(CancellationToken cancellationToken);
}
