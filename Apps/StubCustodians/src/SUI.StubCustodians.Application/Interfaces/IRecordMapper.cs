using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Interfaces
{
    public interface IRecordMapper<in T>
        where T : class
    {
        EventResponse Map(string sui, T sourceRecord);
    }
}
