using SUI.Custodians.Domain.Models;

namespace SUI.StubCustodians.Application.Services;

public class ChildrensServicesDetailsRecordProvider
    : BaseRecordProvider<ChildrensServicesDetailsRecordV1>
{
    public ChildrensServicesDetailsRecordProvider(string? basePath = null)
        : base(basePath) { }
}
