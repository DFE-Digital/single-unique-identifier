using SUI.FakeCustodians.Application.Contracts.Niche;
using SUI.FakeCustodians.Application.Interfaces;
using SUI.FakeCustodians.Application.Models;

namespace SUI.FakeCustodians.Application.Mappers
{
    public class NicheRecordMapper : IRecordMapper<NicheRecord>
    {
        public EventResponse Map(string sui, NicheRecord sourceRecord)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sui, nameof(sui));
            ArgumentNullException.ThrowIfNull(sourceRecord, nameof(sourceRecord));

            return new EventResponse
            {
                Sui = sui,
                Data = new()
                {
                    PersonalData = MapToPersonalData(sourceRecord),
                    PoliceData = MapToPoliceData(sourceRecord),
                    //EducationData = null,
                    //ProbationData = null,
                    //GpData = null,
                    //CamhsData = null
                },
            };
        }

        private PoliceData? MapToPoliceData(NicheRecord source)
        {
            return new PoliceData
            {
                ChildProtection = source.ChildProtection,
                KnownToPolice = source.KnownToPolice,
                PolicePowersOfProtection = source.PolicePowersOfProtection,
            };
        }

        private PersonalData? MapToPersonalData(NicheRecord source)
        {
            return new PersonalData
            {
                FirstName = source.FirstName,
                LastName = source.LastName,
                DateOfBirth = source.DateOfBirth,
                NhsNumber = source.NhsNumber,
            };
        }
    }
}
