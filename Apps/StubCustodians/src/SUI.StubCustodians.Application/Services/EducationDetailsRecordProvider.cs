using SUI.Custodians.Domain.Models;

namespace SUI.StubCustodians.Application.Services;

public class EducationDetailsRecordProvider : BaseRecordProvider<EducationDetailsRecordV1>
{
    public EducationDetailsRecordProvider(string? basePath = null)
        : base(basePath) { }
}
