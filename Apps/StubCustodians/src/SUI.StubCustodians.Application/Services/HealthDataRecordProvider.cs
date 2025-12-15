using SUI.Custodians.Domain.Models;

namespace SUI.StubCustodians.Application.Services;

public class HealthDataRecordProvider : BaseRecordProvider<HealthDataRecordV1>
{
    public HealthDataRecordProvider(string? basePath = null)
        : base(basePath) { }
}
