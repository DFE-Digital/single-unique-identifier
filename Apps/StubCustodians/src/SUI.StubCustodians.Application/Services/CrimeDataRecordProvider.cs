using SUI.Custodians.Domain.Models;

namespace SUI.StubCustodians.Application.Services;

public class CrimeDataRecordProvider : BaseRecordProvider<CrimeDataRecordV1>
{
    public CrimeDataRecordProvider(string? basePath = null)
        : base(basePath) { }
}
