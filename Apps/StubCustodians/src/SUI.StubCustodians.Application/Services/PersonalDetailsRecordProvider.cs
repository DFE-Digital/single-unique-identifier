using SUI.Custodians.Domain.Models;

namespace SUI.StubCustodians.Application.Services;

public class PersonalDetailsRecordProvider : BaseRecordProvider<PersonalDetailsRecordV1>
{
    public PersonalDetailsRecordProvider(string? basePath = null)
        : base(basePath) { }
}
