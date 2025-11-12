using SUI.FakeCustodians.Application.Contracts.Arbor;
using SUI.FakeCustodians.Application.Interfaces;

namespace SUI.FakeCustodians.Application.Services
{
    public class ArborEventRecordProvider : BaseEventRecordProvider<ArborRecord>
    {
        public ArborEventRecordProvider(IRecordMapper<ArborRecord> mapper)
            : base(mapper, "Arbor") { }
    }
}
