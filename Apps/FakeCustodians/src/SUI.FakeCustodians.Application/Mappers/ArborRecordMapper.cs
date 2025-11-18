using SUI.FakeCustodians.Application.Contracts.Arbor;
using SUI.FakeCustodians.Application.Models;

namespace SUI.FakeCustodians.Application.Mappers
{
    public class ArborRecordMapper : BaseRecordMapper<ArborRecord>
    {
        public override EventResponse Map(string sui, ArborRecord sourceRecord)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sui);
            ArgumentNullException.ThrowIfNull(sourceRecord);

            return new EventResponse
            {
                Sui = sui,
                Data = new()
                {
                    PersonalData = MapToPersonalData(sourceRecord),
                    EducationData = MapToEducationData(sourceRecord),
                    //PoliceData = null,
                    //ProbationData = null,
                    //GpData = null,
                    //CamhsData = null
                },
            };
        }

        private static EducationData? MapToEducationData(ArborRecord source)
        {
            return new EducationData
            {
                PupilPremium = source.PupilPremium,
                FreeSchoolMeals = source.FreeSchoolMeals,
                ElectivelyHomeEducated = source.ElectivelyHomeEducated,
                SchoolsAttended = source.SchoolsAttended?.Select(i => MapToSchool(i)).ToArray(),
            };
        }

        private static School MapToSchool(ArborSchool source)
        {
            return new School { Name = source.Name, Address = source.Address };
        }
    }
}
