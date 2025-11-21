using SUI.StubCustodians.Application.Contracts.Mosaic;
using SUI.StubCustodians.Application.Interfaces;

namespace SUI.StubCustodians.Application.Services;

public class MosaicEventRecordProvider : BaseEventRecordProvider<MosaicRecord>
{
    public MosaicEventRecordProvider(IRecordMapper<MosaicRecord> mapper, string? basePath = null)
        : base(mapper, "Mosaic", basePath) { }
}
