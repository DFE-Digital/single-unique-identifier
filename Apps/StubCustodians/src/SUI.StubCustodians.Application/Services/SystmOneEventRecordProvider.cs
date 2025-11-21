using SUI.StubCustodians.Application.Contracts.SystmOne;
using SUI.StubCustodians.Application.Interfaces;

namespace SUI.StubCustodians.Application.Services;

public class SystmOneEventRecordProvider : BaseEventRecordProvider<SystmOneRecord>
{
    public SystmOneEventRecordProvider(
        IRecordMapper<SystmOneRecord> mapper,
        string? basePath = null
    )
        : base(mapper, "SystmOne", basePath) { }
}
