using SUI.FakeCustodians.Application.Contracts.Mosaic;
using SUI.FakeCustodians.Application.Interfaces;
using SUI.FakeCustodians.Application.Models;

namespace SUI.FakeCustodians.Application.Mappers
{
    public class MosaicRecordMapper : IRecordMapper<MosaicRecord>
    {
        public EventResponse Map(string sui, MosaicRecord sourceRecord)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sui, nameof(sui));
            ArgumentNullException.ThrowIfNull(sourceRecord, nameof(sourceRecord));

            return new EventResponse
            {
                Sui = sui,
                Data = new()
                {
                    PersonalData = MapToPersonalData(sourceRecord),
                    CamhsData = MapToCamhsData(sourceRecord),
                    //EducationData = null,
                    //PoliceData = null,
                    //ProbationData = null,
                    //GpData = null
                },
            };
        }

        private CamhsData? MapToCamhsData(MosaicRecord source)
        {
            return new CamhsData
            {
                Referrals = source.Referrals?.Select(i => MapToCahmsReferral(i)).ToArray(),
            };
        }

        private Referral MapToCahmsReferral(MosaicReferral source)
        {
            return new Referral()
            {
                Id = source.Id,
                Date = source.Date,
                Reason = source.Reason,
            };
        }

        private PersonalData? MapToPersonalData(MosaicRecord source)
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
