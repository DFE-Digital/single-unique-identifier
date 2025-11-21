using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Interfaces
{
    public interface IEventRecordProvider
    {
        EventResponse? GetEventRecordForSui(string sui);
    }
}
