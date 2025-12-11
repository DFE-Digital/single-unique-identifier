using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Interfaces;

public interface IRecordProvider<T>
    where T : class
{
    RecordEnvelope<T>? GetRecordForSui(string sui, string providerSystemId);
}
