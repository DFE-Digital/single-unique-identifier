using SUI.StubCustodians.Application.Contracts.Niche;
using SUI.StubCustodians.Application.Interfaces;

namespace SUI.StubCustodians.Application.Services;

public class NicheEventRecordProvider : BaseEventRecordProvider<NicheRecord>
{
    public NicheEventRecordProvider(IRecordMapper<NicheRecord> mapper, string? basePath = null)
        : base(mapper, "Niche", basePath) { }
}
