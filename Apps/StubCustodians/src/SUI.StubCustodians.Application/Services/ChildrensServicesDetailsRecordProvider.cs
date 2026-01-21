using SUI.Custodians.Domain.Models;

namespace SUI.StubCustodians.Application.Services;

public class ChildrensServicesDetailsRecordProvider
    : BaseRecordProvider<ChildrensServicesDetailsRecord>
{
    public ChildrensServicesDetailsRecordProvider(string? basePath = null)
        : base(basePath) { }
}
