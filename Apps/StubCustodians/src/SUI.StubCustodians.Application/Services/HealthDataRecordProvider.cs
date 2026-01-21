using SUI.Custodians.Domain.Models;

namespace SUI.StubCustodians.Application.Services;

public class HealthDataRecordProvider : BaseRecordProvider<HealthDataRecord>
{
    public HealthDataRecordProvider(string? basePath = null)
        : base(basePath) { }
}
