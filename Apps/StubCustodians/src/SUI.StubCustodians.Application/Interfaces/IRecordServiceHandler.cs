using SUI.StubCustodians.Application.Common;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Interfaces;

public interface IRecordServiceHandler<T>
    where T : class
{
    Task<HandlerResult<RecordEnvelope<T>>> GetRecord(string sui, string providerSystemId);
}
