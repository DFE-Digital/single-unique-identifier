using SUI.FakeCustodians.Application.Contracts.Niche;
using SUI.FakeCustodians.Application.Interfaces;

namespace SUI.FakeCustodians.Application.Services;

public class NicheEventRecordProvider : BaseEventRecordProvider<NicheRecord>
{
    public NicheEventRecordProvider(IRecordMapper<NicheRecord> mapper, string? basePath = null)
        : base(mapper, "Niche", basePath) { }
}
