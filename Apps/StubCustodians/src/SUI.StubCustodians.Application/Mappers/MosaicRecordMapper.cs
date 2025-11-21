using SUI.StubCustodians.Application.Contracts.Mosaic;
using SUI.StubCustodians.Application.Models;

namespace SUI.StubCustodians.Application.Mappers
{
    public class MosaicRecordMapper : BaseRecordMapper<MosaicRecord>
    {
        public override EventResponse Map(string sui, MosaicRecord sourceRecord)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sui);
            ArgumentNullException.ThrowIfNull(sourceRecord);

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

        private static CamhsData? MapToCamhsData(MosaicRecord source)
        {
            return new CamhsData
            {
                Referrals = source.Referrals?.Select(MapToCahmsReferral).ToArray(),
            };
        }

        private static Referral MapToCahmsReferral(MosaicReferral source)
        {
            return new Referral()
            {
                Id = source.Id,
                Date = source.Date,
                Reason = source.Reason,
            };
        }
    }
}
