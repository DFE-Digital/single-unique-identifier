using SUI.FakeCustodians.Application.Contracts.Niche;
using SUI.FakeCustodians.Application.Models;

namespace SUI.FakeCustodians.Application.Mappers
{
    public class NicheRecordMapper : BaseRecordMapper<NicheRecord>
    {
        public override EventResponse Map(string sui, NicheRecord sourceRecord)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sui);
            ArgumentNullException.ThrowIfNull(sourceRecord);

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
    }
}
