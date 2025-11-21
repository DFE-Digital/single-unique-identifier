using SUI.StubCustodians.Application.Contracts.SystmOne;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Mappers
{
    public class SystmOneRecordMapper : BaseRecordMapper<SystmOneRecord>
    {
        public override EventResponse Map(string sui, SystmOneRecord sourceRecord)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sui);
            ArgumentNullException.ThrowIfNull(sourceRecord);

            return new EventResponse
            {
                Sui = sui,
                Data = new()
                {
                    PersonalData = MapToPersonalData(sourceRecord),
                    GpData = MapToGpData(sourceRecord),
                    //EducationData = null,
                    //PoliceData = null,
                    //ProbationData = null,
                    //CamhsData = null
                },
            };
        }

        private static GpData? MapToGpData(SystmOneRecord source)
        {
            return new GpData
            {
                GpName = source.GpName,
                GpSurgery = source.GpSurgery,
                GpContactNumber = source.GpContactNumber,
                MissedAppointmentReasons = source
                    .MissedAppointmentReasons?.Select(MapToMissedAppointment)
                    .ToArray(),
            };
        }

        private static MissedAppointment MapToMissedAppointment(SystmOneMissedAppointment source)
        {
            return new MissedAppointment
            {
                Date = source.Date,
                Reason = source.Reason,
                Location = source.Location,
            };
        }
    }
}
