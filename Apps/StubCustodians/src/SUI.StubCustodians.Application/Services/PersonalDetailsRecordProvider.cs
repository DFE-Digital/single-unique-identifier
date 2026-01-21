using SUI.Custodians.Domain.Models;

namespace SUI.StubCustodians.Application.Services;

public class PersonalDetailsRecordProvider : BaseRecordProvider<PersonalDetailsRecord>
{
    public PersonalDetailsRecordProvider(string? basePath = null)
        : base(basePath) { }
}
