using SUI.FakeCustodians.Application.Models;

namespace SUI.FakeCustodians.Application.Interfaces
{
    public interface IEventRecordProvider
    {
        EventResponse? GetEventRecordForSui(string sui);
    }
}