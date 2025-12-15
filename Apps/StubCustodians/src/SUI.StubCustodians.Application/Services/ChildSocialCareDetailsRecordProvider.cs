using SUI.Custodians.Domain.Models;

namespace SUI.StubCustodians.Application.Services;

public class ChildSocialCareDetailsRecordProvider
    : BaseRecordProvider<ChildSocialCareDetailsRecordV1>
{
    public ChildSocialCareDetailsRecordProvider(string? basePath = null)
        : base(basePath) { }
}
