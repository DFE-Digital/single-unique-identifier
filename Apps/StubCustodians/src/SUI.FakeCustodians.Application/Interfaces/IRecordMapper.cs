using SUI.FakeCustodians.Application.Models;

namespace SUI.FakeCustodians.Application.Interfaces
{
    public interface IRecordMapper<in T>
        where T : class
    {
        EventResponse Map(string sui, T sourceRecord);
    }
}
