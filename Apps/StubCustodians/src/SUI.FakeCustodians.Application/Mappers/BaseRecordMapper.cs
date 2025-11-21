using SUI.FakeCustodians.Application.Contracts;
using SUI.FakeCustodians.Application.Interfaces;
using SUI.FakeCustodians.Application.Models;

namespace SUI.FakeCustodians.Application.Mappers
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
