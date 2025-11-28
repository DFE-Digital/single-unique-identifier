namespace SUI.Transfer.Application.Services;

public interface ITransferJob // rs-todo: implement, and write unit tests
{
    Task TransferAsync(Guid jobId, string sui);
}
