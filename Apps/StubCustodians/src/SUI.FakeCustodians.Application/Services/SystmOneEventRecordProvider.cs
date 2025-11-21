using SUI.FakeCustodians.Application.Contracts.SystmOne;
using SUI.FakeCustodians.Application.Interfaces;

namespace SUI.FakeCustodians.Application.Services;

public class SystmOneEventRecordProvider : BaseEventRecordProvider<SystmOneRecord>
{
    public SystmOneEventRecordProvider(
        IRecordMapper<SystmOneRecord> mapper,
        string? basePath = null
    )
        : base(mapper, "SystmOne", basePath) { }
}
