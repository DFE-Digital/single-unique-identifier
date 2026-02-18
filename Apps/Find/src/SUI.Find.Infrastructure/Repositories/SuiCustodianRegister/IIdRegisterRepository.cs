namespace SUI.Find.Infrastructure.Repositories.SuiCustodianRegister;

public interface IIdRegisterRepository
{
    Task UpsertAsync(IdRegisterEntry registerEntry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IdRegisterEntry>> GetEntriesBySuiAsync(
        string sui,
        CancellationToken cancellationToken = default
    );
}
