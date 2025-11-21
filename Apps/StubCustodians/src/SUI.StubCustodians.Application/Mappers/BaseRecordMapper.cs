using SUI.StubCustodians.Application.Contracts;
using SUI.StubCustodians.Application.Interfaces;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Mappers
{
    public abstract class BaseRecordMapper<T> : IRecordMapper<T>
        where T : BaseEntity
    {
        public abstract EventResponse Map(string sui, T sourceRecord);

        protected static PersonalData MapToPersonalData(BaseEntity baseSourceRecord)
        {
            ArgumentNullException.ThrowIfNull(baseSourceRecord);

            return new PersonalData
            {
                FirstName = baseSourceRecord.FirstName,
                LastName = baseSourceRecord.LastName,
                DateOfBirth = baseSourceRecord.DateOfBirth,
                NhsNumber = baseSourceRecord.NhsNumber,
            };
        }
    }
}
