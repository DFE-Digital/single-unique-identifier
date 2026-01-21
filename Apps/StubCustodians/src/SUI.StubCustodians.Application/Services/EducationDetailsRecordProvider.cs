using SUI.Custodians.Domain.Models;

namespace SUI.StubCustodians.Application.Services;

public class EducationDetailsRecordProvider : BaseRecordProvider<EducationDetailsRecord>
{
    public EducationDetailsRecordProvider(string? basePath = null)
        : base(basePath) { }
}
