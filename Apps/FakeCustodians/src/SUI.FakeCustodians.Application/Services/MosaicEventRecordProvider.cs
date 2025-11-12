using SUI.FakeCustodians.Application.Contracts.Mosaic;
using SUI.FakeCustodians.Application.Interfaces;

namespace SUI.FakeCustodians.Application.Services;

public class MosaicEventRecordProvider : BaseEventRecordProvider<MosaicRecord>
{
    public MosaicEventRecordProvider(IRecordMapper<MosaicRecord> mapper)
        : base(mapper, "Mosaic") { }
}
