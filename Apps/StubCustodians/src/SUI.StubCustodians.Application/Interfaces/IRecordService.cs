using SUI.Custodians.Domain.Models;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Interfaces;

public interface IRecordService
{
    Task<RecordEnvelope<T>?> GetRecord<T>(string personId, string orgId)
        where T : class;
}
