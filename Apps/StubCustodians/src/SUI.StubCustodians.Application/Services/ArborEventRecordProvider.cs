using SUI.StubCustodians.Application.Contracts.Arbor;
using SUI.StubCustodians.Application.Interfaces;

namespace SUI.StubCustodians.Application.Services
{
    public class ArborEventRecordProvider : BaseEventRecordProvider<ArborRecord>
    {
        public ArborEventRecordProvider(IRecordMapper<ArborRecord> mapper, string? basePath = null)
            : base(mapper, "Arbor", basePath) { }
    }
}
