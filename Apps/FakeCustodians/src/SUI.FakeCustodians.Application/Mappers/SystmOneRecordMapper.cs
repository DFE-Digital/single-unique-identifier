using SUI.FakeCustodians.Application.Contracts.SystmOne;
using SUI.FakeCustodians.Application.Interfaces;
using SUI.FakeCustodians.Application.Models;

namespace SUI.FakeCustodians.Application.Mappers
{
    public class SystmOneRecordMapper : IRecordMapper<SystmOneRecord>
    {
        public EventResponse Map(string sui, SystmOneRecord sourceRecord)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sui, nameof(sui));
            ArgumentNullException.ThrowIfNull(sourceRecord, nameof(sourceRecord));

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

        private GpData? MapToGpData(SystmOneRecord source)
        {
            return new GpData
            {
                GpName = source.GpName,
                GpSurgery = source.GpSurgery,
                GpContactNumber = source.GpContactNumber,
                MissedAppointmentReasons = source
                    .MissedAppointmentReasons?.Select(i => MapToMissedAppointment(i))
                    .ToArray(),
            };
        }

        private MissedAppointment MapToMissedAppointment(SystmOneMissedAppointment source)
        {
            return new MissedAppointment
            {
                Date = source.Date,
                Reason = source.Reason,
                Location = source.Location,
            };
        }

        private PersonalData? MapToPersonalData(SystmOneRecord source)
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
