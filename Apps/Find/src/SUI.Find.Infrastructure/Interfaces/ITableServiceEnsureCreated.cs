namespace SUI.Find.Infrastructure.Interfaces;

public interface ITableServiceEnsureCreated
{
    Task EnsureTableExistsAsync(CancellationToken cancellationToken);
}
